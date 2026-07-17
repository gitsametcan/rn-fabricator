namespace Fabricator.Core.Workspaces;

public sealed record WorkspaceDiscoveryResult(
    string WorkspacePath,
    bool WorkspaceExists,
    WorkspaceTemplateCatalog TemplateCatalog,
    IReadOnlyList<WorkspaceProjectCandidate> Projects);
