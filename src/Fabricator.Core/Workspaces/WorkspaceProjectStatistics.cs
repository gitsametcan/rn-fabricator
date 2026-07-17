namespace Fabricator.Core.Workspaces;

public sealed record WorkspaceProjectStatistics(
    int SourceFileCount,
    int ScreenFileCount,
    int ComponentFileCount,
    int ServiceFileCount,
    int UtilityFileCount);
