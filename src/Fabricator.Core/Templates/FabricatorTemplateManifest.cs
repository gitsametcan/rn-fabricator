namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateManifest(
    int SchemaVersion,
    string Kind,
    string Id,
    string DisplayName,
    string Description,
    string Version,
    string Mode,
    IReadOnlyList<FabricatorTemplateFile> Files,
    string? Category = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<FabricatorTemplateDependency>? Dependencies = null,
    IReadOnlyList<FabricatorTemplateExport>? Exports = null,
    IReadOnlyList<FabricatorTemplateIntegrationHint>? IntegrationHints = null);
