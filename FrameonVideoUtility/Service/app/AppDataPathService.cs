using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrameonVideoUtility.Service.App;

public static class AppDataPathService
{
    public static string GetWritableFrameOnDataDirectory()
    {
        foreach (string candidate in GetFrameOnDataDirectoryCandidates().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (TryEnsureWritableDirectory(candidate))
            {
                return candidate;
            }
        }

        string tempFallback = Path.Combine(Path.GetTempPath(), "FrameOn");
        Directory.CreateDirectory(tempFallback);
        return tempFallback;
    }

    public static IEnumerable<string> GetFrameOnDataDirectoryCandidates()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "FrameOn");
        }

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
        {
            yield return Path.Combine(appData, "FrameOn");
        }

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            if (OperatingSystem.IsMacOS())
            {
                yield return Path.Combine(userProfile, "Library", "Caches", "FrameOn");
            }
            else if (OperatingSystem.IsLinux())
            {
                yield return Path.Combine(userProfile, ".cache", "FrameOn");
            }
        }
    }

    private static bool TryEnsureWritableDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            string probePath = Path.Combine(directory, $".frameon-write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probePath, string.Empty);
            File.Delete(probePath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
