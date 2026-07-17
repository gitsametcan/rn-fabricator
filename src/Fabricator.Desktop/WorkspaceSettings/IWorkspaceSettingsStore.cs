namespace Fabricator.Desktop.WorkspaceSettings;

public interface IWorkspaceSettingsStore
{
    string? LoadLastWorkspacePath();

    void SaveLastWorkspacePath(string workspacePath);
}
