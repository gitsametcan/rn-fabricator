namespace Fabricator.Core.Workspaces;

public interface IWorkspaceDiscoveryService
{
    WorkspaceDiscoveryResult Discover(string workspacePath);
}
