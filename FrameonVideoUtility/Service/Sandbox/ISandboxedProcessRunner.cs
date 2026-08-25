using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FrameonVideoUtility.Service.Sandbox;

public interface ISandboxedProcessRunner
{
    Task<SandboxedProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string outputDirectory,
        string tempDirectory,
        Action<string>? onOutput,
        Action<string>? onError,
        CancellationToken cancellationToken);
}
