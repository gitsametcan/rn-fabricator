namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateUpdateResult
{
    public FabricatorTemplateUpdateResult(
        string templateId,
        string catalogPath,
        string templateDirectory,
        string manifestPath,
        IReadOnlyList<string> addedFiles,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> unchangedFiles,
        IReadOnlyList<string> removedFiles,
        IReadOnlyList<string> preservedMetadata,
        IReadOnlyList<string> errors,
        bool dryRun = false)
    {
        TemplateId = templateId;
        CatalogPath = catalogPath;
        TemplateDirectory = templateDirectory;
        ManifestPath = manifestPath;
        AddedFiles = addedFiles;
        ChangedFiles = changedFiles;
        UnchangedFiles = unchangedFiles;
        RemovedFiles = removedFiles;
        PreservedMetadata = preservedMetadata;
        Errors = errors;
        DryRun = dryRun;
    }

    public string TemplateId { get; }

    public string CatalogPath { get; }

    public string TemplateDirectory { get; }

    public string ManifestPath { get; }

    public IReadOnlyList<string> AddedFiles { get; }

    public IReadOnlyList<string> ChangedFiles { get; }

    public IReadOnlyList<string> UnchangedFiles { get; }

    public IReadOnlyList<string> RemovedFiles { get; }

    public IReadOnlyList<string> PreservedMetadata { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool DryRun { get; }

    public bool Succeeded => Errors.Count == 0;
}
