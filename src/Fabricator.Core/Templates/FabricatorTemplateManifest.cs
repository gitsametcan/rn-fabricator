namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateManifest(
    int SchemaVersion,
    string Kind,
    string Id,
    string DisplayName,
    string Description,
    string Version,
    string Mode,
    IReadOnlyList<FabricatorTemplateFile> Files);
