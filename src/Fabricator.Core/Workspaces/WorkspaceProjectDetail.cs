namespace Fabricator.Core.Workspaces;

public sealed record WorkspaceProjectDetail(
    string ProjectPath,
    bool ProjectExists,
    string DisplayName,
    string? PackageName,
    bool HasFabricatorManifest,
    bool HasFabricatorState,
    int AppliedTemplateCount,
    WorkspaceProjectStatistics Statistics,
    WorkspaceStoreMetadata StoreMetadata);
