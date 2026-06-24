namespace Fabricator.Core.Projects;

public sealed class CreateProjectStarterResult
{
    private CreateProjectStarterResult(
        string starterId,
        string starterVersion,
        string category,
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> errors)
    {
        StarterId = starterId;
        StarterVersion = starterVersion;
        Category = category;
        GeneratedFiles = generatedFiles;
        Errors = errors;
    }

    public string StarterId { get; }

    public string StarterVersion { get; }

    public string Category { get; }

    public IReadOnlyList<string> GeneratedFiles { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static CreateProjectStarterResult Applied(
        string starterId,
        IReadOnlyList<string> generatedFiles)
    {
        return Applied(starterId, "0.1.0", "starter", generatedFiles);
    }

    public static CreateProjectStarterResult Applied(
        string starterId,
        string starterVersion,
        string category,
        IReadOnlyList<string> generatedFiles)
    {
        return new CreateProjectStarterResult(starterId, starterVersion, category, generatedFiles, []);
    }

    public static CreateProjectStarterResult Failed(
        string starterId,
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> errors)
    {
        return Failed(starterId, "0.1.0", "starter", generatedFiles, errors);
    }

    public static CreateProjectStarterResult Failed(
        string starterId,
        string starterVersion,
        string category,
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> errors)
    {
        return new CreateProjectStarterResult(starterId, starterVersion, category, generatedFiles, errors);
    }
}
