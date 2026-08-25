using System;
using System.IO;
using System.Linq;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Service.Sandbox;

public static class SandboxPathService
{
    public static string CreateJobId(string prefix)
    {
        return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
    }

    public static string GetSandboxJobFolder(string jobId, string childFolder)
    {
        string folder = IOPath.Combine(
            GetFrameOnSandboxRoot(),
            jobId,
            childFolder
        );

        Directory.CreateDirectory(folder);
        return folder;
    }

    public static string GetFrameOnSandboxRoot()
    {
        string folder = IOPath.Combine(
            IOPath.GetTempPath(),
            "FrameOnSandbox"
        );

        Directory.CreateDirectory(folder);
        return folder;
    }

    public static void ClearFolder(string folder)
    {
        Directory.CreateDirectory(folder);

        foreach (string file in Directory.GetFiles(folder))
        {
            File.Delete(file);
        }

        foreach (string directory in Directory.GetDirectories(folder))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static string[] MoveSandboxFilesToOutputFolder(string sandboxFolder, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        string[] files = Directory.GetFiles(sandboxFolder);

        if (files.Length == 0)
        {
            throw new FileNotFoundException("No output files were created.");
        }

        string[] destinations = new string[files.Length];

        for (int index = 0; index < files.Length; index++)
        {
            string file = files[index];
            string destination = GetNonConflictingPath(
                IOPath.Combine(outputFolder, IOPath.GetFileName(file))
            );

            File.Move(file, destination);
            destinations[index] = destination;
        }

        return destinations;
    }

    public static void MoveSingleSandboxDownloadToFinalPath(string sandboxFolder, string finalOutputPath)
    {
        string[] files = Directory.GetFiles(sandboxFolder);

        if (files.Length == 0)
        {
            throw new FileNotFoundException("No downloaded output file was created.");
        }

        string finalExtension = IOPath.GetExtension(finalOutputPath);
        string? sourceFile = files.FirstOrDefault(file =>
            string.Equals(IOPath.GetExtension(file), finalExtension, StringComparison.OrdinalIgnoreCase));

        if (sourceFile is null)
        {
            string availableFiles = string.Join(", ", files.Select(IOPath.GetFileName));

            throw new InvalidDataException(
                $"Downloaded output did not match the selected file type {finalExtension}. Available output: {availableFiles}"
            );
        }

        if (File.Exists(finalOutputPath))
        {
            File.Delete(finalOutputPath);
        }

        File.Move(sourceFile, finalOutputPath);
    }

    private static string GetNonConflictingPath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        string directory = IOPath.GetDirectoryName(path)!;
        string fileName = IOPath.GetFileNameWithoutExtension(path);
        string extension = IOPath.GetExtension(path);

        int counter = 1;

        while (true)
        {
            string candidate = IOPath.Combine(directory, $"{fileName} ({counter}){extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }
}
