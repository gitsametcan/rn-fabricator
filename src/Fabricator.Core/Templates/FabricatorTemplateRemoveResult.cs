namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateRemoveResult
{
    public FabricatorTemplateRemoveResult(
        string templateId,
        string catalogPath,
        string? templateDirectory,
        bool catalogEntryRemoved,
        bool templateFilesDeleted,
        IReadOnlyList<string> errors)
    {
        TemplateId = templateId;
        CatalogPath = catalogPath;
        TemplateDirectory = templateDirectory;
        CatalogEntryRemoved = catalogEntryRemoved;
        TemplateFilesDeleted = templateFilesDeleted;
        Errors = errors;
    }

    public string TemplateId { get; }

    public string CatalogPath { get; }

    public string? TemplateDirectory { get; }

    public bool CatalogEntryRemoved { get; }

    public bool TemplateFilesDeleted { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded => Errors.Count == 0;
}
