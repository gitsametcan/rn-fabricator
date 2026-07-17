using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using Fabricator.Core.Workspaces;
using Fabricator.Desktop.ViewModels;
using Fabricator.Desktop.WorkspaceSettings;
using Fabricator.Desktop.Views;

namespace Fabricator.Tests.Desktop;

public sealed class DesktopShellSmokeTests
{
    [AvaloniaFact]
    public void MainWindowRendersStableShell()
    {
        var window = new MainWindow
        {
            DataContext = new MainViewModel(
                new WorkspaceDiscoveryService(),
                new MemoryWorkspaceSettingsStore())
        };

        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);

        var visibleText = window
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        Assert.Equal("rn-fabricator", window.Title);
        Assert.IsType<MainViewModel>(window.DataContext);
        Assert.Contains("rn-fabricator", visibleText);
        Assert.Contains("Workspace", visibleText);
        Assert.Contains("Workspace root", visibleText);
        Assert.Contains("Mobile projects", visibleText);
        Assert.Contains("No workspace selected", visibleText);
        Assert.Contains("Load workspace", visibleText);
    }

    [Fact]
    public void MainViewModelStartsFromEmptyWorkspaceState()
    {
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new MemoryWorkspaceSettingsStore());

        Assert.Equal("Workspace", viewModel.PageTitle);
        Assert.False(viewModel.HasWorkspace);
        Assert.False(viewModel.HasTemplateCatalog);
        Assert.Empty(viewModel.Projects);
        Assert.Equal("No workspace selected", viewModel.SelectedWorkspacePath);
        Assert.Equal("Load a workspace to discover mobile projects.", viewModel.EmptyProjectListMessage);
        Assert.Equal(4, viewModel.NavigationItems.Count);
        Assert.Contains(viewModel.NavigationItems, item => item.Title == "Workspace");
        Assert.Contains(viewModel.NavigationItems, item => item.Title == "Doctor");
        Assert.Contains(viewModel.NavigationItems, item => item.Title == "Templates");
    }

    [Fact]
    public void MainViewModelReloadsRememberedWorkspace()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        CreateFabricatorProject(workspace.Path, "FabricatorApp");
        CreateReactNativeProject(workspace.Path, "ExistingMobileApp");
        var settings = new MemoryWorkspaceSettingsStore(workspace.Path);

        var viewModel = new MainViewModel(new WorkspaceDiscoveryService(), settings);

        Assert.True(viewModel.HasWorkspace);
        Assert.True(viewModel.HasTemplateCatalog);
        Assert.Equal(Path.GetFullPath(workspace.Path), viewModel.SelectedWorkspacePath);
        Assert.Equal(2, viewModel.Projects.Count);
        Assert.Contains(viewModel.Projects, project =>
            project.Name == "FabricatorApp" &&
            project.Status == "Ready");
        Assert.Contains(viewModel.Projects, project =>
            project.Name == "ExistingMobileApp" &&
            project.Status == "Missing Fabricator state");
    }

    [Fact]
    public void MainViewModelLoadsWorkspaceFromInputAndSavesPath()
    {
        using var workspace = new TemporaryDirectory();
        CreateReactNativeProject(workspace.Path, "ExistingMobileApp");
        var settings = new MemoryWorkspaceSettingsStore();
        var viewModel = new MainViewModel(new WorkspaceDiscoveryService(), settings)
        {
            WorkspaceInputPath = workspace.Path
        };

        viewModel.LoadWorkspaceCommand.Execute(null);

        Assert.True(viewModel.HasWorkspace);
        Assert.Equal(Path.GetFullPath(workspace.Path), viewModel.SelectedWorkspacePath);
        Assert.Equal(Path.GetFullPath(workspace.Path), settings.SavedWorkspacePath);
        Assert.Single(viewModel.Projects);
    }

    [Fact]
    public void MainViewModelLoadsSelectedProjectDetail()
    {
        using var workspace = new TemporaryDirectory();
        CreateFabricatorProject(workspace.Path, "FabricatorApp");
        WriteFile(workspace.Path, "FabricatorApp/src/screens/HomeScreen.tsx");
        WriteFile(workspace.Path, "FabricatorApp/AGENTS.md");
        WriteFile(workspace.Path, "FabricatorApp/.agents/current-focus.md", "Build the workspace UI.\n");
        WriteFile(workspace.Path, "FabricatorApp/.agents/handoff.md", "## Open Questions\n\n- Keep going?\n");
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path));
        var project = Assert.Single(viewModel.Projects);

        viewModel.SelectProjectCommand.Execute(project);

        Assert.True(viewModel.HasSelectedProject);
        Assert.NotNull(viewModel.SelectedProjectDetail);
        Assert.Equal("FabricatorApp", viewModel.SelectedProjectDetail.DisplayName);
        Assert.Equal("Fabricator-compatible", viewModel.SelectedProjectDetail.FabricatorStatus);
        Assert.Equal("fabricator-app", viewModel.SelectedProjectDetail.PackageName);
        Assert.Equal("0 applied template(s)", viewModel.SelectedProjectDetail.AppliedTemplateCount);
        Assert.Equal("1 source file(s)", viewModel.SelectedProjectDetail.SourceFileCount);
        Assert.Equal("1 screen file(s)", viewModel.SelectedProjectDetail.ScreenFileCount);
        Assert.Equal("Missing", viewModel.SelectedProjectDetail.AppStoreName);
        Assert.Equal("Missing", viewModel.SelectedProjectDetail.PlayStoreName);
        Assert.Equal("Configured", viewModel.SelectedProjectDetail.AgentMemoryStatus);
        Assert.Equal("AGENTS.md found", viewModel.SelectedProjectDetail.AgentInstructionsStatus);
        Assert.Equal(".agents found", viewModel.SelectedProjectDetail.AgentDirectoryStatus);
        Assert.StartsWith("handoff.md found", viewModel.SelectedProjectDetail.AgentHandoffStatus);
        Assert.Equal("current-focus.md found", viewModel.SelectedProjectDetail.AgentCurrentFocusStatus);
        Assert.Equal("Build the workspace UI.", viewModel.SelectedProjectDetail.AgentCurrentFocusSummary);
        Assert.Equal("1 open question(s)", viewModel.SelectedProjectDetail.AgentOpenQuestionCount);
    }

    private static void CreateTemplateCatalog(string workspacePath)
    {
        var catalogPath = Path.Combine(workspacePath, TemplateSourceResolver.ConventionalCatalogRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(catalogPath)!);
        File.WriteAllText(catalogPath, "{}");
    }

    private static void CreateFabricatorProject(string workspacePath, string name)
    {
        var projectPath = CreateReactNativeProject(workspacePath, name);
        Directory.CreateDirectory(Path.Combine(projectPath, ".fabricator"));
        File.WriteAllText(Path.Combine(projectPath, FabricatorProjectContract.ManifestRelativePath), "{}");
        File.WriteAllText(
            Path.Combine(projectPath, FabricatorProjectStateContract.StateRelativePath),
            $$"""
            {
              "project": {
                "name": "{{name}}"
              },
              "appliedTemplates": []
            }
            """);
    }

    private static string CreateReactNativeProject(string workspacePath, string name)
    {
        var projectPath = Path.Combine(workspacePath, name);
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(
            Path.Combine(projectPath, "package.json"),
            """
            {
              "name": "fabricator-app",
              "dependencies": {
                "react-native": "0.76.0"
              }
            }
            """);

        return projectPath;
    }

    private static void WriteFile(string workspacePath, string relativePath, string contents = "export {};\n")
    {
        var path = Path.Combine(workspacePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private sealed class MemoryWorkspaceSettingsStore : IWorkspaceSettingsStore
    {
        private readonly string? _lastWorkspacePath;

        public MemoryWorkspaceSettingsStore(string? lastWorkspacePath = null)
        {
            _lastWorkspacePath = lastWorkspacePath;
        }

        public string? SavedWorkspacePath { get; private set; }

        public string? LoadLastWorkspacePath()
        {
            return _lastWorkspacePath;
        }

        public void SaveLastWorkspacePath(string workspacePath)
        {
            SavedWorkspacePath = Path.GetFullPath(workspacePath);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"fabricator-desktop-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
