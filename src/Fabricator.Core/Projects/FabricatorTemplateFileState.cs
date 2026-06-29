namespace Fabricator.Core.Projects;

public sealed record FabricatorTemplateFileState(
    IReadOnlyList<string> Written,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Overwritten);
