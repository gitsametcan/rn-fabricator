namespace Fabricator.Core.Templates;

public sealed record TemplateApplicationRequest(
    TemplatePackage Package,
    string TargetDirectory,
    bool OverwriteExistingFiles = false);
