namespace Fabricator.Core.Projects;

public sealed class CreateProjectStarterResult
{
    private CreateProjectStarterResult(
        string starterId,
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> errors)
    {
        StarterId = starterId;
        GeneratedFiles = generatedFiles;
        Errors = errors;
    }

    public string StarterId { get; }

    public IReadOnlyList<string> GeneratedFiles { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static CreateProjectStarterResult Applied(
        string starterId,
        IReadOnlyList<string> generatedFiles)
    {
        return new CreateProjectStarterResult(starterId, generatedFiles, []);
    }

    public static CreateProjectStarterResult Failed(
        string starterId,
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> errors)
    {
        return new CreateProjectStarterResult(starterId, generatedFiles, errors);
    }
}
