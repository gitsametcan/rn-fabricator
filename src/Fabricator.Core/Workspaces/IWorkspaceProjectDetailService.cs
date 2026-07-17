namespace Fabricator.Core.Workspaces;

public interface IWorkspaceProjectDetailService
{
    WorkspaceProjectDetail GetDetail(string projectPath);
}
