using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Fabricator.Core;
using Fabricator.Core.Environment;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;
using Fabricator.Core.Setup;
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
    public void MainWindowRendersNavigationItemsAsCommandButtons()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        var window = new MainWindow
        {
            DataContext = new MainViewModel(
                new WorkspaceDiscoveryService(),
                new MemoryWorkspaceSettingsStore(workspace.Path))
        };

        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);

        var buttons = window
            .GetVisualDescendants()
            .OfType<Button>()
            .ToArray();

        Assert.Contains(buttons, button => ButtonContainsText(button, "Workspace") && button.Command is not null);
        Assert.Contains(buttons, button => ButtonContainsText(button, "Templates") && button.Command is not null);
        Assert.Contains(buttons, button => ButtonContainsText(button, "Create Project") && button.Command is not null);
        Assert.Contains(buttons, button => ButtonContainsText(button, "Doctor") && button.Command is not null);
        Assert.Contains(buttons, button => ButtonContainsText(button, "Setup") && button.Command is not null);
    }

    [AvaloniaFact]
    public void MainWindowKeepsMainContentVerticallyScrollable()
    {
        var window = new MainWindow
        {
            DataContext = new MainViewModel(
                new WorkspaceDiscoveryService(),
                new MemoryWorkspaceSettingsStore())
        };

        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);

        Assert.Contains(
            window.GetVisualDescendants().OfType<ScrollViewer>(),
            scrollViewer =>
                scrollViewer.VerticalScrollBarVisibility == ScrollBarVisibility.Auto &&
                scrollViewer.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled);
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
        Assert.Contains("Native dependencies", visibleText);
        Assert.Contains("Install CocoaPods during create", visibleText);
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
        Assert.Contains("CocoaPods", visibleText);
        Assert.Contains("Skip during create", visibleText);
        Assert.Contains("Readiness", visibleText);
        Assert.Contains("Doctor has not run for this session. Run Doctor before creating when you need environment confidence.", visibleText);
        Assert.Contains("Run Doctor", visibleText);
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
        Assert.False(viewModel.IsDoctorView);
        Assert.False(viewModel.IsSetupView);
        Assert.False(viewModel.HasDoctorResults);
        Assert.False(viewModel.HasSetupPlanItems);
        Assert.False(viewModel.IsCreateConfirmed);
        Assert.Equal(string.Empty, viewModel.CreateOutputDirectory);
        Assert.Equal(CreateProjectService.DefaultStarterId, viewModel.CreateTemplateName);
        Assert.Equal(string.Empty, viewModel.CreateTemplateSource);
        Assert.False(viewModel.CreateInstallPods);
        Assert.Equal("Skip during create", viewModel.CreateInstallPodsLabel);
        Assert.Equal("Load a workspace before creating a project.", viewModel.CreateFormStatus);
    }

    [Fact]
    public async Task MainViewModelNavigatesBackToWorkspaceFromSecondaryViews()
    {
        using var workspace = new TemporaryDirectory();
        CreateTemplateCatalog(workspace.Path);
        var createService = new RecordingCreateProjectService(
            BuildCreateProjectResult("UnusedApp", workspace.Path, ExitCodes.Success, string.Empty, string.Empty));
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService,
            CreateDoctorService(),
            CreateSetupPlanService());

        viewModel.ShowTemplatesCommand.Execute(null);
        Assert.True(viewModel.IsTemplatesView);
        viewModel.ShowWorkspaceCommand.Execute(null);
        Assert.True(viewModel.IsWorkspaceView);

        viewModel.ShowCreateCommand.Execute(null);
        Assert.True(viewModel.IsCreateView);
        viewModel.ShowWorkspaceCommand.Execute(null);
        Assert.True(viewModel.IsWorkspaceView);

        await viewModel.ShowDoctorCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsDoctorView);
        viewModel.ShowWorkspaceCommand.Execute(null);
        Assert.True(viewModel.IsWorkspaceView);

        await viewModel.ShowSetupCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsSetupView);
        viewModel.ShowWorkspaceCommand.Execute(null);
        Assert.True(viewModel.IsWorkspaceView);
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
        Assert.False(viewModel.CreateInstallPods);
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
        Assert.False(viewModel.CreateInstallPods);
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
                ["@react-native-community/cli@latest", "init", "RunApp", "--install-pods", "false"],
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
        Assert.False(request.InstallPods);
        Assert.True(viewModel.IsCreateReviewStep);
        Assert.True(viewModel.IsCreateConfirmed);
        Assert.False(viewModel.IsCreateRunning);
        Assert.True(viewModel.HasCreateRun);
        Assert.True(viewModel.IsCreateSucceeded);
        Assert.False(viewModel.IsCreateFailed);
        Assert.Equal("Succeeded", viewModel.CreateExecutionState);
        Assert.Equal("npx @react-native-community/cli@latest init RunApp --install-pods false --working-directory=" + workspace.Path, viewModel.CreatePreparedCommand);
        Assert.Equal("scaffolded\n", viewModel.CreateStandardOutput);
        Assert.Equal("warning\n", viewModel.CreateStandardError);
        Assert.Equal(Path.Combine(Path.GetFullPath(workspace.Path), "RunApp"), viewModel.CreateTargetProjectPath);
        Assert.Equal(
            $"Created project at {Path.Combine(Path.GetFullPath(workspace.Path), "RunApp")}. Starter: {CreateProjectService.DefaultStarterId} (1 file(s)).",
            viewModel.CreateResultSummary);
        Assert.Equal("Create completed successfully.", viewModel.CreateFormStatus);
    }

    [Fact]
    public async Task MainViewModelRefreshesWorkspaceAfterSuccessfulCreate()
    {
        using var workspace = new TemporaryDirectory();
        var result = BuildCreateProjectResult(
            "RefreshApp",
            workspace.Path,
            ExitCodes.Success,
            "completed\n",
            string.Empty);
        var createService = new RecordingCreateProjectService(result)
        {
            BeforeReturn = _ => CreateFabricatorProject(workspace.Path, "RefreshApp")
        };
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService)
        {
            CreateProjectName = "RefreshApp"
        };

        Assert.Empty(viewModel.Projects);

        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);
        await viewModel.ConfirmCreateCommand.ExecuteAsync(null);

        var project = Assert.Single(viewModel.Projects);
        Assert.Equal("RefreshApp", project.Name);
        Assert.True(viewModel.HasSelectedProject);
        Assert.NotNull(viewModel.SelectedProjectDetail);
        Assert.Equal("RefreshApp", viewModel.SelectedProjectDetail.DisplayName);
        Assert.Equal("Workspace refreshed after creating RefreshApp.", viewModel.WorkspaceStatus);
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
        Assert.Contains("Rollback:", viewModel.CreateResultSummary);
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
        Assert.False(viewModel.CreateInstallPods);
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
        Assert.False(viewModel.CreateInstallPods);
        Assert.Equal("Load an existing workspace before creating a project.", viewModel.CreateFormStatus);
    }

    [Fact]
    public async Task MainViewModelPassesCocoaPodsSelectionToCreateRequest()
    {
        using var workspace = new TemporaryDirectory();
        var result = BuildCreateProjectResult(
            "PodsApp",
            workspace.Path,
            ExitCodes.Success,
            "completed\n",
            string.Empty);
        var createService = new RecordingCreateProjectService(result);
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService)
        {
            CreateProjectName = "PodsApp",
            CreateInstallPods = true
        };

        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);
        await viewModel.ConfirmCreateCommand.ExecuteAsync(null);

        var request = Assert.Single(createService.Requests);
        Assert.True(request.InstallPods);
        Assert.Equal("Install during create", viewModel.CreateInstallPodsLabel);
    }

    [AvaloniaFact]
    public async Task MainWindowRendersDoctorView()
    {
        using var workspace = new TemporaryDirectory();
        var dependencyCheckService = CreateDoctorService();
        var createService = new RecordingCreateProjectService(
            BuildCreateProjectResult("UnusedApp", workspace.Path, ExitCodes.Success, string.Empty, string.Empty));
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService,
            dependencyCheckService);

        await viewModel.ShowDoctorCommand.ExecuteAsync(null);

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

        Assert.Contains("Doctor", visibleText);
        Assert.Contains("Environment checks", visibleText);
        Assert.Contains("Core tools", visibleText);
        Assert.Contains("Apple tools", visibleText);
        Assert.Contains("Android tools", visibleText);
        Assert.Contains("Node.js", visibleText);
        Assert.Contains("Watchman", visibleText);
        Assert.Contains("Android SDK", visibleText);
        Assert.Contains("Install Watchman.", visibleText);
        Assert.Contains("Install Android Studio.", visibleText);
        Assert.Contains("Run Doctor", visibleText);
        Assert.Contains("Back to workspace", visibleText);
    }

    [Fact]
    public async Task MainViewModelRunsDoctorChecks()
    {
        using var workspace = new TemporaryDirectory();
        var dependencyCheckService = CreateDoctorService();
        var createService = new RecordingCreateProjectService(
            BuildCreateProjectResult("UnusedApp", workspace.Path, ExitCodes.Success, string.Empty, string.Empty));
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService,
            dependencyCheckService);

        await viewModel.ShowDoctorCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsDoctorView);
        Assert.False(viewModel.IsWorkspaceView);
        Assert.True(viewModel.HasDoctorRun);
        Assert.False(viewModel.IsDoctorRunning);
        Assert.True(viewModel.HasDoctorResults);
        Assert.Equal(3, viewModel.DoctorGroups.Count);
        Assert.Equal("Passed 1, warnings 1, failed 1.", viewModel.DoctorSummaryLabel);
        Assert.Equal("Doctor found required tools that need attention.", viewModel.DoctorStatus);
        Assert.Contains(viewModel.DoctorGroups, group => group.Title == "Core tools");
        Assert.Contains(viewModel.DoctorGroups, group => group.Title == "Apple tools");
        Assert.Contains(viewModel.DoctorGroups, group => group.Title == "Android tools");
    }

    [Fact]
    public async Task MainViewModelShowsDoctorReadinessOnCreateReview()
    {
        using var workspace = new TemporaryDirectory();
        var createService = new RecordingCreateProjectService(
            BuildCreateProjectResult("ReviewApp", workspace.Path, ExitCodes.Success, string.Empty, string.Empty));
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService,
            CreateDoctorService(),
            CreateSetupPlanService())
        {
            CreateProjectName = "ReviewApp"
        };

        viewModel.ShowCreateCommand.Execute(null);
        viewModel.ReviewCreateCommand.Execute(null);

        Assert.Contains("Doctor has not run", viewModel.CreateReadinessStatus);
        Assert.Equal("Run Doctor", viewModel.CreateReadinessActionLabel);

        await viewModel.RunDoctorCommand.ExecuteAsync(null);

        Assert.Equal("Run Doctor again", viewModel.CreateReadinessActionLabel);
        Assert.Contains("Doctor found required tools that need attention.", viewModel.CreateReadinessStatus);
        Assert.Contains("Passed 1, warnings 1, failed 1.", viewModel.CreateReadinessStatus);
    }

    [AvaloniaFact]
    public async Task MainWindowRendersSetupPlanView()
    {
        using var workspace = new TemporaryDirectory();
        var setupPlanService = CreateSetupPlanService();
        var createService = new RecordingCreateProjectService(
            BuildCreateProjectResult("UnusedApp", workspace.Path, ExitCodes.Success, string.Empty, string.Empty));
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService,
            CreateDoctorService(),
            setupPlanService);

        await viewModel.ShowSetupCommand.ExecuteAsync(null);

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

        Assert.Contains("Setup", visibleText);
        Assert.Contains("Setup plan", visibleText);
        Assert.Contains("Build Plan", visibleText);
        Assert.Contains("Platform: macOS", visibleText);
        Assert.Contains("Package manager: Homebrew (brew)", visibleText);
        Assert.Contains("Toolchain profile: React Native Stable (0.76.x)", visibleText);
        Assert.Contains("Node.js", visibleText);
        Assert.Contains("Watchman", visibleText);
        Assert.Contains("ANDROID_HOME", visibleText);
        Assert.Contains("brew install node", visibleText);
    }

    [Fact]
    public async Task MainViewModelBuildsSetupPlanPreview()
    {
        using var workspace = new TemporaryDirectory();
        var setupPlanService = CreateSetupPlanService();
        var createService = new RecordingCreateProjectService(
            BuildCreateProjectResult("UnusedApp", workspace.Path, ExitCodes.Success, string.Empty, string.Empty));
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path),
            createService,
            CreateDoctorService(),
            setupPlanService);

        await viewModel.ShowSetupCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsSetupView);
        Assert.False(viewModel.IsWorkspaceView);
        Assert.True(viewModel.HasSetupPlanRun);
        Assert.False(viewModel.IsSetupPlanRunning);
        Assert.True(viewModel.HasSetupPlanItems);
        Assert.Equal(3, viewModel.SetupPlanItems.Count);
        Assert.Equal("Commands 1, manual 1, environment 1.", viewModel.SetupPlanSummaryLabel);
        Assert.Equal("Review the setup plan before running any install commands.", viewModel.SetupPlanStatus);
        Assert.Equal("Platform: macOS", viewModel.SetupPlanPlatformLabel);
        Assert.Equal("Package manager: Homebrew (brew)", viewModel.SetupPlanPackageManagerLabel);
        Assert.Equal("Toolchain profile: React Native Stable (0.76.x)", viewModel.SetupPlanToolchainLabel);
        Assert.Equal(1, setupPlanService.CallCount);
        Assert.Equal(SetupPlanRequest.Default, setupPlanService.LastRequest);
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
        Assert.Equal("Command center metadata missing", viewModel.SelectedProjectDetail.CommandCenterStatus);
        Assert.Equal("No publishing metadata yet.", viewModel.SelectedProjectDetail.PublishingSummary);
        Assert.Equal("No market research notes yet.", viewModel.SelectedProjectDetail.ResearchSummary);
        Assert.Equal("No release checklist yet.", viewModel.SelectedProjectDetail.ReleaseChecklistSummary);
        Assert.Equal("No next actions yet.", viewModel.SelectedProjectDetail.NextActionsSummary);
        Assert.Contains(viewModel.SelectedProjectDetail.PublishingReadinessItems, item =>
            item.Label == "iOS bundle id" &&
            item.Status == "Missing");
        Assert.Contains(viewModel.SelectedProjectDetail.PublishingReadinessItems, item =>
            item.Label == "Release metadata" &&
            item.Status == "Missing");
        Assert.Contains(viewModel.SelectedProjectDetail.ResearchItems, item =>
            item.Label == "Target audience" &&
            item.Status == "Missing");
        Assert.Contains(viewModel.SelectedProjectDetail.ResearchItems, item =>
            item.Label == "Keywords" &&
            item.Detail == "No local entries.");
        Assert.Contains(viewModel.SelectedProjectDetail.ReleaseChecklistItems, item =>
            item.Label == "Release checklist" &&
            item.Status == "Missing");
    }

    [AvaloniaFact]
    public void MainWindowRendersApplicationCommandCenterShell()
    {
        using var workspace = new TemporaryDirectory();
        CreateFabricatorProject(workspace.Path, "CommandCenterApp");
        WriteFile(
            workspace.Path,
            "CommandCenterApp/.fabricator/app-command-center.json",
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-app-command-center",
              "publishing": {
                "ios": {
                  "displayName": "Command Center iOS",
                  "identifier": "com.example.commandcenter.ios",
                  "version": "1.0.0",
                  "buildNumber": "10",
                  "status": "ready"
                },
                "android": {
                  "displayName": "Command Center Android",
                  "identifier": "com.example.commandcenter.android",
                  "version": "1.0.0",
                  "buildNumber": "11",
                  "status": "missing"
                },
                "releaseOwner": "Mobile Team"
              },
              "marketResearch": {
                "targetAudience": "Busy professionals",
                "positioning": "Habit tracking for mobile-first users",
                "keywords": ["fitness", "habit"],
                "competitors": ["Competitor A"],
                "openQuestions": ["Which geography first?"],
                "notes": "Validate paid acquisition before launch."
              },
              "releaseChecklist": {
                "items": [
                  {
                    "id": "privacy-policy",
                    "title": "Privacy policy",
                    "status": "ready",
                    "notes": "Published and linked."
                  },
                  {
                    "id": "screenshots",
                    "title": "Store screenshots",
                    "status": "missing",
                    "notes": "Need localized screenshots."
                  }
                ]
              },
              "nextActions": [
                {
                  "id": "screenshots",
                  "title": "Prepare screenshots",
                  "group": "publishing",
                  "status": "open"
                }
              ]
            }
            """);
        var viewModel = new MainViewModel(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new MemoryWorkspaceSettingsStore(workspace.Path));
        var project = Assert.Single(viewModel.Projects);
        viewModel.SelectProjectCommand.Execute(project);
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

        Assert.Contains("Application Command Center", visibleText);
        Assert.Contains("Command center metadata loaded", visibleText);
        Assert.Contains("Publishing", visibleText);
        Assert.Contains("iOS: ready; Android: missing", visibleText);
        Assert.Contains("iOS bundle id", visibleText);
        Assert.Contains("com.example.commandcenter.ios", visibleText);
        Assert.Contains("Android application id", visibleText);
        Assert.Contains("com.example.commandcenter.android", visibleText);
        Assert.Contains("Release owner", visibleText);
        Assert.Contains("Mobile Team", visibleText);
        Assert.Contains("Research", visibleText);
        Assert.Contains("2 keyword(s), 1 competitor(s), 1 open question(s)", visibleText);
        Assert.Contains("Target audience", visibleText);
        Assert.Contains("Busy professionals", visibleText);
        Assert.Contains("Positioning", visibleText);
        Assert.Contains("Habit tracking for mobile-first users", visibleText);
        Assert.Contains("Keywords", visibleText);
        Assert.Contains("fitness, habit", visibleText);
        Assert.Contains("Open questions", visibleText);
        Assert.Contains("Which geography first?", visibleText);
        Assert.Contains("Research notes", visibleText);
        Assert.Contains("Validate paid acquisition before launch.", visibleText);
        Assert.Contains("Release", visibleText);
        Assert.Contains("2 release checklist item(s)", visibleText);
        Assert.Contains("Privacy policy", visibleText);
        Assert.Contains("Published and linked.", visibleText);
        Assert.Contains("Store screenshots", visibleText);
        Assert.Contains("Need localized screenshots.", visibleText);
        Assert.Contains("Next Actions", visibleText);
        Assert.Contains("1 next action(s)", visibleText);
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

    private static bool ButtonContainsText(Button button, string text)
    {
        return button
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .Any(textBlock => string.Equals(textBlock.Text, text, StringComparison.Ordinal));
    }

    private static FakeDependencyCheckService CreateDoctorService()
    {
        return new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary([
                DependencyCheckResult.Passed("Node.js", "v20.11.1", "Node.js is installed.")
            ]),
            AppleToolsSummary = new DependencyCheckSummary([
                DependencyCheckResult.Warning("Watchman", null, "Watchman was not found.", "Install Watchman.")
            ]),
            AndroidToolsSummary = new DependencyCheckSummary([
                DependencyCheckResult.Failed("Android SDK", null, "Android SDK was not found.", "Install Android Studio.")
            ])
        };
    }

    private static FakeSetupPlanService CreateSetupPlanService()
    {
        return new FakeSetupPlanService
        {
            Plan = new SetupPlan(
                "macOS",
                new PackageManagerInfo("Homebrew", true, "brew"),
                [
                    new SetupPlanItem(
                        "Node.js",
                        SetupPlanItemKind.Command,
                        "Install Node.js",
                        ["brew install node"]),
                    new SetupPlanItem(
                        "Watchman",
                        SetupPlanItemKind.Manual,
                        "Install Watchman manually",
                        ["Open the Watchman install guide."]),
                    new SetupPlanItem(
                        "ANDROID_HOME",
                        SetupPlanItemKind.Environment,
                        "Set Android SDK environment variables",
                        ["Export ANDROID_HOME in your shell profile."])
                ],
                new SetupPlanToolchainProfile(
                    "react-native-stable",
                    "React Native Stable",
                    "0.76.x",
                    true))
        };
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

        public Action<CreateProjectRequest>? BeforeReturn { get; init; }

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

            BeforeReturn?.Invoke(request);

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
