namespace FrameonVideoUtility.Service.Sandbox;

public sealed class SandboxedProcessResult
{
    public int ExitCode { get; init; }
    public bool WasCanceled { get; init; }
    public string[] OutputFiles { get; init; } = [];
}
