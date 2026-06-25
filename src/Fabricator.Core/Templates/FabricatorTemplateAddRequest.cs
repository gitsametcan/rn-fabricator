namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateAddRequest(
    string TemplateId,
    string Category,
    string SourceProjectDirectory,
    string CatalogSource,
    bool DryRun = false);
