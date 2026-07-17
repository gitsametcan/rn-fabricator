namespace Fabricator.Core.Workspaces;

public sealed record WorkspaceProjectCandidate(
    string Name,
    string Path,
    WorkspaceProjectKind Kind,
    bool HasFabricatorManifest,
    bool HasFabricatorState,
    bool HasPackageJson,
    bool HasAppJson,
    bool HasIosDirectory,
    bool HasAndroidDirectory);
