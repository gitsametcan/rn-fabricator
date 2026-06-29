namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateFile(
    string Path,
    string Type,
    string? TargetPath = null,
    string? TargetFolder = null,
    string? Description = null);
