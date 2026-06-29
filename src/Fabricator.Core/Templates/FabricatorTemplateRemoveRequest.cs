namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateRemoveRequest(
    string TemplateId,
    string CatalogSource,
    bool DeleteFiles,
    bool DryRun = false);
