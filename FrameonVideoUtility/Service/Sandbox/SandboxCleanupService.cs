using System;
using System.IO;
using FrameonVideoUtility.Service.Logging;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Service.Sandbox;

public static class SandboxCleanupService
{
    public static void CleanupSandbox()
    {
        try
        {
            string sandboxRoot = IOPath.Combine(
                IOPath.GetTempPath(),
                "FrameOnSandbox"
            );

            if (!Directory.Exists(sandboxRoot))
            {
                return;
            }

            foreach (string directory in Directory.GetDirectories(sandboxRoot))
            {
                if (string.Equals(IOPath.GetFileName(directory), "Tools", StringComparison.OrdinalIgnoreCase))
                {
                    AppLog.Info($"[Sandbox] Preserved durable tools folder: {directory}");
                    continue;
                }

                Directory.Delete(directory, recursive: true);
                AppLog.Info($"[Sandbox] Cleaned sandbox job folder: {directory}");
            }

            foreach (string file in Directory.GetFiles(sandboxRoot))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error($"[Sandbox] Cleanup failed: {ex.Message}");
        }
    }
}
