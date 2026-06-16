namespace Fabricator.Core.Processes;

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
