namespace Fabricator.Core.Templates;

public sealed record TemplateManifest(
    string Id,
    string DisplayName,
    string Description,
    string Version,
    IReadOnlyList<string> Files);
