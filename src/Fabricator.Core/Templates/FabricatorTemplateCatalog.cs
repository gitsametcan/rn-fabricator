namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateCatalog(
    int SchemaVersion,
    string Kind,
    string DisplayName,
    string Description,
    IReadOnlyList<FabricatorTemplateCatalogEntry> Templates);
