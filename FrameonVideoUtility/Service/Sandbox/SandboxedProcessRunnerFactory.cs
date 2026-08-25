using System;
using System.Runtime.InteropServices;

namespace FrameonVideoUtility.Service.Sandbox;

public static class SandboxedProcessRunnerFactory
{
    public static ISandboxedProcessRunner Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsSandboxRunner();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacProcessRunner();
        }

        throw new PlatformNotSupportedException("Only Windows and macOS are supported.");
    }
}
