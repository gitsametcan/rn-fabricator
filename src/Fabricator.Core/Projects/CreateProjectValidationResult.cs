namespace Fabricator.Core.Projects;

public sealed class CreateProjectValidationResult
{
    public CreateProjectValidationResult(
        CreateProjectRequest request,
        string fullOutputDirectory,
        string fullProjectPath,
        IReadOnlyList<string> errors)
    {
        Request = request;
        FullOutputDirectory = fullOutputDirectory;
        FullProjectPath = fullProjectPath;
        Errors = errors;
    }

    public CreateProjectRequest Request { get; }

    public string FullOutputDirectory { get; }

    public string FullProjectPath { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}
