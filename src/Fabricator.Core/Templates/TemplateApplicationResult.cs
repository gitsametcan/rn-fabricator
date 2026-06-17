namespace Fabricator.Core.Templates;

public sealed class TemplateApplicationResult
{
    public TemplateApplicationResult(
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> skippedFiles,
        IReadOnlyList<string> errors)
    {
        GeneratedFiles = generatedFiles;
        SkippedFiles = skippedFiles;
        Errors = errors;
    }

    public IReadOnlyList<string> GeneratedFiles { get; }

    public IReadOnlyList<string> SkippedFiles { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded => Errors.Count == 0;
}
