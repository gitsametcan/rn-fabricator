namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateCatalogEntry(
    string Id,
    string DisplayName,
    string Description,
    string Version,
    string Manifest,
    IReadOnlyList<string> Tags,
    string? Category = null);
