namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateCaptureResult
{
    public FabricatorTemplateCaptureResult(
        string templateId,
        string templateDirectory,
        string manifestPath,
        IReadOnlyList<string> capturedFiles,
        IReadOnlyList<string> errors,
        bool dryRun = false)
    {
        TemplateId = templateId;
        TemplateDirectory = templateDirectory;
        ManifestPath = manifestPath;
        CapturedFiles = capturedFiles;
        Errors = errors;
        DryRun = dryRun;
    }

    public string TemplateId { get; }

    public string TemplateDirectory { get; }

    public string ManifestPath { get; }

    public IReadOnlyList<string> CapturedFiles { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool DryRun { get; }

    public bool Succeeded => Errors.Count == 0;
}
