namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateAddResult
{
    public FabricatorTemplateAddResult(
        string templateId,
        string catalogPath,
        string templateDirectory,
        string manifestPath,
        IReadOnlyList<string> capturedFiles,
        IReadOnlyList<string> errors,
        bool dryRun = false)
    {
        TemplateId = templateId;
        CatalogPath = catalogPath;
        TemplateDirectory = templateDirectory;
        ManifestPath = manifestPath;
        CapturedFiles = capturedFiles;
        Errors = errors;
        DryRun = dryRun;
    }

    public string TemplateId { get; }

    public string CatalogPath { get; }

    public string TemplateDirectory { get; }

    public string ManifestPath { get; }

    public IReadOnlyList<string> CapturedFiles { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool DryRun { get; }

    public bool Succeeded => Errors.Count == 0;
}
