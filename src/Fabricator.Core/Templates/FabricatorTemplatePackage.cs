namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplatePackage(
    FabricatorTemplateManifest Manifest,
    IReadOnlyDictionary<string, string> Files);
