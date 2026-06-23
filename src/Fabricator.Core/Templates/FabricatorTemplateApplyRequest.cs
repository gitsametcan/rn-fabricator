namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateApplyRequest(
    FabricatorTemplatePackage Package,
    string TargetDirectory,
    bool OverwriteExistingFiles);
