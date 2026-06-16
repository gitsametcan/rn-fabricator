namespace Fabricator.Core.Processes;

public sealed record ProcessRunRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null)
{
    public static ProcessRunRequest Create(
        string fileName,
        params string[] arguments)
    {
        return new ProcessRunRequest(fileName, arguments);
    }
}
