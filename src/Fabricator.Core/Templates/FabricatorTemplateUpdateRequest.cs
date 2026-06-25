namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateUpdateRequest(
    string TemplateId,
    string SourceProjectDirectory,
    string CatalogSource,
    bool DryRun = false);
