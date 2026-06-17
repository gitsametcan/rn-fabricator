namespace Fabricator.Core.Templates;

public sealed record TemplatePackage(
    TemplateManifest Manifest,
    string RootDirectory);
