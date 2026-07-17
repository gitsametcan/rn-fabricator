using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Fabricator.Core;
using Fabricator.Core.Processes;
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
        Assert.Contains("Create Project", visibleText);
    }

    [AvaloniaFact]
    public void MainWindowRendersCreateProjectForm()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new MemoryWorkspaceSettingsStore(workspace.Path));
        viewModel.ShowCreateCommand.Execute(null);
        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);

        var visibleText = window
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        Assert.Contains("Create Project", visibleText);
        Assert.Contains("Project details", visibleText);
        Assert.Contains("Project name", visibleText);
        Assert.Contains("Output directory", visibleText);
        Assert.Contains("Starter template", visibleText);
        Assert.Contains("Template source", visibleText);
        Assert.Contains("Back to workspace", visibleText);
        Assert.Contains("Review create", visibleText);
    }

    [AvaloniaFact]
    public void MainWindowRendersCreateProjectReview()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new MemoryWorkspaceSettingsStore(workspace.Path))
        {
            CreateProjectName = "ReviewApp"
        };
        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);
        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);

        var visibleText = window
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        Assert.Contains("Review", visibleText);
        Assert.Contains("ReviewApp", visibleText);
        Assert.Contains(Path.Combine(Path.GetFullPath(workspace.Path), "ReviewApp"), visibleText);
        Assert.Contains(CreateProjectService.DefaultStarterId, visibleText);
        Assert.Contains("Back to edit", visibleText);
        Assert.Contains("Confirm create", visibleText);
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
        Assert.False(viewModel.IsCreateFormEnabled);
        Assert.True(viewModel.IsCreateEditStep);
        Assert.False(viewModel.IsCreateReviewStep);
        Assert.False(viewModel.IsCreateConfirmed);
        Assert.Equal(string.Empty, viewModel.CreateOutputDirectory);
        Assert.Equal(CreateProjectService.DefaultStarterId, viewModel.CreateTemplateName);
        Assert.Equal(string.Empty, viewModel.CreateTemplateSource);
        Assert.Equal("Load a workspace before creating a project.", viewModel.CreateFormStatus);
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
        Assert.True(viewModel.HasTemplateGroups);
        Assert.Equal(2, viewModel.TemplateGroups.Count);
        Assert.Equal(Path.GetFullPath(workspace.Path), viewModel.SelectedWorkspacePath);
        Assert.Equal(2, viewModel.Projects.Count);
        Assert.Contains(viewModel.Projects, project =>
            project.Name == "FabricatorApp" &&
            project.Status == "Ready");
        Assert.Contains(viewModel.Projects, project =>
            project.Name == "ExistingMobileApp" &&
            project.Status == "Missing Fabricator state");
        Assert.True(viewModel.IsCreateFormEnabled);
        Assert.Equal(Path.GetFullPath(workspace.Path), viewModel.CreateOutputDirectory);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(workspace.Path, TemplateSourceResolver.ConventionalCatalogRelativePath)),
            viewModel.CreateTemplateSource);
    }

    [Fact]
    public void MainViewModelShowsCreateProjectFormDefaultsFromWorkspace()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path));

        viewModel.ShowCreateCommand.Execute(null);

        Assert.True(viewModel.IsCreateView);
        Assert.False(viewModel.IsWorkspaceView);
        Assert.False(viewModel.IsTemplatesView);
        Assert.True(viewModel.IsCreateEditStep);
        Assert.False(viewModel.IsCreateReviewStep);
        Assert.True(viewModel.IsCreateFormEnabled);
        Assert.Equal(string.Empty, viewModel.CreateProjectName);
        Assert.Equal(Path.GetFullPath(workspace.Path), viewModel.CreateOutputDirectory);
        Assert.Equal(CreateProjectService.DefaultStarterId, viewModel.CreateTemplateName);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(workspace.Path, TemplateSourceResolver.ConventionalCatalogRelativePath)),
            viewModel.CreateTemplateSource);
        Assert.Equal(
            "Ready to configure a new project. The workspace template catalog is selected.",
            viewModel.CreateFormStatus);
    }

    [Fact]
    public void MainViewModelMovesCreateProjectFormToReviewAndBack()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path))
        {
            CreateProjectName = "ReviewApp"
        };

        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);

        Assert.True(viewModel.IsCreateReviewStep);
        Assert.False(viewModel.IsCreateEditStep);
        Assert.False(viewModel.IsCreateConfirmed);
        Assert.Equal(Path.Combine(Path.GetFullPath(workspace.Path), "ReviewApp"), viewModel.CreateTargetProjectPath);
        Assert.Equal(
            "Review the target path and create inputs before confirmation.",
            viewModel.CreateFormStatus);

        viewModel.EditCreateCommand.Execute(null);

        Assert.False(viewModel.IsCreateReviewStep);
        Assert.True(viewModel.IsCreateEditStep);
        Assert.False(viewModel.IsCreateConfirmed);
        Assert.Equal("Ready to edit create inputs.", viewModel.CreateFormStatus);
    }

    [Fact]
    public void MainViewModelRequiresCreateProjectNameBeforeReview()
    {
        using var workspace = new TemporaryDirectory();
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path));

        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);

        Assert.False(viewModel.IsCreateReviewStep);
        Assert.True(viewModel.IsCreateEditStep);
        Assert.Equal("Project name is required before review.", viewModel.CreateFormStatus);
    }

    [Fact]
    public async Task MainViewModelRunsCreateThroughFakeServiceAndCapturesOutput()
    {
        using var workspace = new TemporaryDirectory();
        var result = BuildCreateProjectResult(
            "RunApp",
            workspace.Path,
            ExitCodes.Success,
            "completed\n",
            string.Empty);
        var createService = new RecordingCreateProjectService(result)
        {
            PreparedCommand = new ProcessRunRequest(
                "npx",
                ["@react-native-community/cli@latest", "init", "RunApp"],
                workspace.Path),
            StandardOutputChunk = "scaffolded\n",
            StandardErrorChunk = "warning\n"
        };
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService)
        {
            CreateProjectName = "RunApp"
        };

        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);
        await viewModel.ConfirmCreateCommand.ExecuteAsync(null);

        var request = Assert.Single(createService.Requests);
        Assert.Equal("RunApp", request.ProjectName);
        Assert.Equal(CreateProjectService.DefaultStarterId, request.TemplateName);
        Assert.Equal(Path.GetFullPath(workspace.Path), request.OutputDirectory);
        Assert.True(viewModel.IsCreateReviewStep);
        Assert.True(viewModel.IsCreateConfirmed);
        Assert.False(viewModel.IsCreateRunning);
        Assert.True(viewModel.HasCreateRun);
        Assert.True(viewModel.IsCreateSucceeded);
        Assert.False(viewModel.IsCreateFailed);
        Assert.Equal("Succeeded", viewModel.CreateExecutionState);
        Assert.Equal("npx @react-native-community/cli@latest init RunApp --working-directory=" + workspace.Path, viewModel.CreatePreparedCommand);
        Assert.Equal("scaffolded\n", viewModel.CreateStandardOutput);
        Assert.Equal("warning\n", viewModel.CreateStandardError);
        Assert.Equal(Path.Combine(Path.GetFullPath(workspace.Path), "RunApp"), viewModel.CreateTargetProjectPath);
        Assert.Equal($"Created project at {Path.Combine(Path.GetFullPath(workspace.Path), "RunApp")}", viewModel.CreateResultSummary);
        Assert.Equal("Create completed successfully.", viewModel.CreateFormStatus);
    }

    [Fact]
    public async Task MainViewModelRepresentsCreateFailureFromFakeService()
    {
        using var workspace = new TemporaryDirectory();
        var result = BuildCreateProjectResult(
            "FailApp",
            workspace.Path,
            ExitCodes.GeneralFailure,
            string.Empty,
            "create failed");
        var createService = new RecordingCreateProjectService(result)
        {
            PreparedCommand = new ProcessRunRequest("npx", ["init", "FailApp"], workspace.Path),
            StandardErrorChunk = "create failed"
        };
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService)
        {
            CreateProjectName = "FailApp"
        };

        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);
        await viewModel.ConfirmCreateCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasCreateRun);
        Assert.False(viewModel.IsCreateSucceeded);
        Assert.True(viewModel.IsCreateFailed);
        Assert.Equal("Failed", viewModel.CreateExecutionState);
        Assert.Equal("create failed", viewModel.CreateStandardError);
        Assert.Contains("Process failed with exit code", viewModel.CreateResultSummary);
        Assert.Contains("create failed", viewModel.CreateResultSummary);
        Assert.Equal("Create failed. Review the result and logs.", viewModel.CreateFormStatus);
    }

    [Fact]
    public void MainViewModelHandlesCreateProjectFormWithoutWorkspace()
    {
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore());

        viewModel.ShowCreateCommand.Execute(null);

        Assert.True(viewModel.IsCreateView);
        Assert.False(viewModel.IsWorkspaceView);
        Assert.False(viewModel.IsTemplatesView);
        Assert.False(viewModel.IsCreateFormEnabled);
        Assert.Equal(string.Empty, viewModel.CreateProjectName);
        Assert.Equal(string.Empty, viewModel.CreateOutputDirectory);
        Assert.Equal(CreateProjectService.DefaultStarterId, viewModel.CreateTemplateName);
        Assert.Equal(string.Empty, viewModel.CreateTemplateSource);
        Assert.Equal("Load an existing workspace before creating a project.", viewModel.CreateFormStatus);
    }

    [Fact]
    public void MainViewModelHandlesCreateProjectFormWithMissingWorkspace()
    {
        using var workspace = new TemporaryDirectory();
        var missingWorkspace = Path.Combine(workspace.Path, "missing");
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore())
        {
            WorkspaceInputPath = missingWorkspace
        };

        viewModel.LoadWorkspaceCommand.Execute(null);
        viewModel.ShowCreateCommand.Execute(null);

        Assert.False(viewModel.HasWorkspace);
        Assert.True(viewModel.IsCreateView);
        Assert.False(viewModel.IsCreateFormEnabled);
        Assert.Equal(string.Empty, viewModel.CreateOutputDirectory);
        Assert.Equal(CreateProjectService.DefaultStarterId, viewModel.CreateTemplateName);
        Assert.Equal(string.Empty, viewModel.CreateTemplateSource);
        Assert.Equal("Load an existing workspace before creating a project.", viewModel.CreateFormStatus);
    }

    [Fact]
    public void MainViewModelShowsGroupedTemplateCatalogView()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path));

        viewModel.ShowTemplatesCommand.Execute(null);

        Assert.True(viewModel.IsTemplatesView);
        Assert.False(viewModel.IsWorkspaceView);
        Assert.Equal("2 categor(ies)", viewModel.TemplateGroupCountLabel);
        var componentGroup = Assert.Single(viewModel.TemplateGroups, group => group.Category == "component");
        var screenGroup = Assert.Single(viewModel.TemplateGroups, group => group.Category == "screen");
        Assert.Equal("1 template(s)", componentGroup.CountLabel);
        Assert.Equal("Primary Button", Assert.Single(componentGroup.Templates).DisplayName);
        Assert.Equal("Settings Screen", Assert.Single(screenGroup.Templates).DisplayName);
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
        File.WriteAllText(
            catalogPath,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-template-catalog",
              "displayName": "Test catalog",
              "description": "Catalog for desktop tests.",
              "templates": [
                {
                  "id": "component/primary-button",
                  "displayName": "Primary Button",
                  "description": "Reusable button.",
                  "version": "0.1.0",
                  "category": "component",
                  "manifest": "component/primary-button/fabricator-template.json",
                  "tags": ["component", "button"]
                },
                {
                  "id": "screen/settings-screen",
                  "displayName": "Settings Screen",
                  "description": "Reusable settings screen.",
                  "version": "0.1.0",
                  "category": "screen",
                  "manifest": "screen/settings-screen/fabricator-template.json",
                  "tags": ["screen", "settings"]
                }
              ]
            }
            """);
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

    private static CreateProjectResult BuildCreateProjectResult(
        string projectName,
        string outputDirectory,
        int exitCode,
        string standardOutput,
        string standardError)
    {
        var request = new CreateProjectRequest(
            projectName,
            CreateProjectService.DefaultStarterId,
            outputDirectory);
        var validation = new CreateProjectValidator().Validate(request);
        var command = new ProcessRunRequest("npx", ["init", projectName], outputDirectory);
        var processResult = new ProcessRunResult(exitCode, standardOutput, standardError);
        var starterResult = exitCode == ExitCodes.Success
            ? CreateProjectStarterResult.Applied(CreateProjectService.DefaultStarterId, ["App.tsx"])
            : null;

        return CreateProjectResult.Completed(
            validation,
            command,
            processResult,
            CreateProjectRollbackResult.NotRequired("Rollback was not required."),
            starterResult);
    }

    private sealed class RecordingCreateProjectService : ICreateProjectService
    {
        private readonly CreateProjectResult _result;

        public RecordingCreateProjectService(CreateProjectResult result)
        {
            _result = result;
        }

        public List<CreateProjectRequest> Requests { get; } = [];

        public ProcessRunRequest? PreparedCommand { get; init; }

        public string StandardOutputChunk { get; init; } = string.Empty;

        public string StandardErrorChunk { get; init; } = string.Empty;

        public Task<CreateProjectResult> CreateAsync(
            CreateProjectRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            request.OnCommandPrepared?.Invoke(
                PreparedCommand ?? new ProcessRunRequest("npx", ["init", request.ProjectName], request.OutputDirectory));

            if (!string.IsNullOrEmpty(StandardOutputChunk))
            {
                request.OnStandardOutput?.Invoke(StandardOutputChunk);
            }

            if (!string.IsNullOrEmpty(StandardErrorChunk))
            {
                request.OnStandardError?.Invoke(StandardErrorChunk);
            }

            return Task.FromResult(_result);
        }
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
