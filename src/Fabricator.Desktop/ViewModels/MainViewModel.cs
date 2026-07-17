using Fabricator.Core;
using Fabricator.Core.Environment;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;
using Fabricator.Core.Setup;
using Fabricator.Core.Templates;
using Fabricator.Core.Toolchains;
using Fabricator.Core.Workspaces;
using Fabricator.Desktop.WorkspaceSettings;
using CommunityToolkit.Mvvm.Input;
using System.Text;
using System.Text.Json;

namespace Fabricator.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IWorkspaceDiscoveryService _workspaceDiscoveryService;
    private readonly IWorkspaceProjectDetailService _workspaceProjectDetailService;
    private readonly IWorkspaceSettingsStore _workspaceSettingsStore;
    private readonly ICreateProjectService _createProjectService;
    private readonly IDependencyCheckService _dependencyCheckService;
    private readonly ISetupPlanService _setupPlanService;
    private string _workspaceInputPath = string.Empty;
    private string _selectedWorkspacePath = "No workspace selected";
    private string _workspaceStatus = "Select a workspace directory to begin.";
    private string _templateCatalogStatus = "No workspace loaded.";
    private bool _hasWorkspace;
    private bool _hasTemplateCatalog;
    private IReadOnlyList<WorkspaceProjectItemViewModel> _projects = [];
    private IReadOnlyList<TemplateCategoryGroupViewModel> _templateGroups = [];
    private WorkspaceProjectItemViewModel? _selectedProject;
    private WorkspaceProjectDetailViewModel? _selectedProjectDetail;
    private bool _isTemplatesView;
    private bool _isCreateView;
    private bool _isDoctorView;
    private bool _isSetupView;
    private string _createProjectName = string.Empty;
    private string _createOutputDirectory = string.Empty;
    private string _createTemplateName = CreateProjectService.DefaultStarterId;
    private string _createTemplateSource = string.Empty;
    private bool _createInstallPods;
    private string _createFormStatus = "Load a workspace before creating a project.";
    private bool _isCreateReviewStep;
    private bool _isCreateConfirmed;
    private bool _isCreateRunning;
    private bool _hasCreateRun;
    private bool _isCreateSucceeded;
    private bool _isCreateFailed;
    private string _createExecutionState = "Idle";
    private string _createPreparedCommand = "No command prepared.";
    private string _createStandardOutput = string.Empty;
    private string _createStandardError = string.Empty;
    private string _createResultSummary = "Create has not run.";
    private IReadOnlyList<DoctorCheckGroupViewModel> _doctorGroups = [];
    private bool _isDoctorRunning;
    private bool _hasDoctorRun;
    private string _doctorStatus = "Run Doctor to check the local React Native toolchain.";
    private string _doctorSummaryLabel = "No checks have run.";
    private IReadOnlyList<SetupPlanItemViewModel> _setupPlanItems = [];
    private bool _isSetupPlanRunning;
    private bool _hasSetupPlanRun;
    private string _setupPlanStatus = "Build a read-only setup plan for missing React Native dependencies.";
    private string _setupPlanSummaryLabel = "No setup plan has run.";
    private string _setupPlanPlatformLabel = "Platform not checked.";
    private string _setupPlanPackageManagerLabel = "Package manager not checked.";
    private string _setupPlanToolchainLabel = "Toolchain profile not checked.";

    public MainViewModel()
        : this(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new FileWorkspaceSettingsStore(),
            CreateDefaultCreateProjectService(),
            CreateDefaultDependencyCheckService(),
            CreateDefaultSetupPlanService())
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceSettingsStore workspaceSettingsStore)
        : this(
            workspaceDiscoveryService,
            new WorkspaceProjectDetailService(),
            workspaceSettingsStore,
            CreateDefaultCreateProjectService(),
            CreateDefaultDependencyCheckService(),
            CreateDefaultSetupPlanService())
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceProjectDetailService workspaceProjectDetailService,
        IWorkspaceSettingsStore workspaceSettingsStore)
        : this(
            workspaceDiscoveryService,
            workspaceProjectDetailService,
            workspaceSettingsStore,
            CreateDefaultCreateProjectService(),
            CreateDefaultDependencyCheckService(),
            CreateDefaultSetupPlanService())
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceProjectDetailService workspaceProjectDetailService,
        IWorkspaceSettingsStore workspaceSettingsStore,
        ICreateProjectService createProjectService)
        : this(
            workspaceDiscoveryService,
            workspaceProjectDetailService,
            workspaceSettingsStore,
            createProjectService,
            CreateDefaultDependencyCheckService(),
            CreateDefaultSetupPlanService())
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceProjectDetailService workspaceProjectDetailService,
        IWorkspaceSettingsStore workspaceSettingsStore,
        ICreateProjectService createProjectService,
        IDependencyCheckService dependencyCheckService)
        : this(
            workspaceDiscoveryService,
            workspaceProjectDetailService,
            workspaceSettingsStore,
            createProjectService,
            dependencyCheckService,
            CreateDefaultSetupPlanService())
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceProjectDetailService workspaceProjectDetailService,
        IWorkspaceSettingsStore workspaceSettingsStore,
        ICreateProjectService createProjectService,
        IDependencyCheckService dependencyCheckService,
        ISetupPlanService setupPlanService)
    {
        _workspaceDiscoveryService = workspaceDiscoveryService;
        _workspaceProjectDetailService = workspaceProjectDetailService;
        _workspaceSettingsStore = workspaceSettingsStore;
        _createProjectService = createProjectService;
        _dependencyCheckService = dependencyCheckService;
        _setupPlanService = setupPlanService;
        LoadWorkspaceCommand = new RelayCommand(LoadWorkspaceFromInput);
        SelectProjectCommand = new RelayCommand<WorkspaceProjectItemViewModel>(SelectProject);
        ClearProjectSelectionCommand = new RelayCommand(() => SelectProject(null));
        ShowWorkspaceCommand = new RelayCommand(ShowWorkspace);
        ShowTemplatesCommand = new RelayCommand(ShowTemplates);
        ShowCreateCommand = new RelayCommand(ShowCreate);
        ShowDoctorCommand = new AsyncRelayCommand(ShowDoctorAsync);
        RunDoctorCommand = new AsyncRelayCommand(RunDoctorAsync);
        ShowSetupCommand = new AsyncRelayCommand(ShowSetupAsync);
        RunSetupPlanCommand = new AsyncRelayCommand(RunSetupPlanAsync);
        ReviewCreateCommand = new RelayCommand(ReviewCreate);
        EditCreateCommand = new RelayCommand(EditCreate);
        ConfirmCreateCommand = new AsyncRelayCommand(ConfirmCreateAsync);

        var lastWorkspacePath = _workspaceSettingsStore.LoadLastWorkspacePath();
        if (!string.IsNullOrWhiteSpace(lastWorkspacePath))
        {
            WorkspaceInputPath = lastWorkspacePath;
            LoadWorkspace(lastWorkspacePath, save: false);
        }
    }

    public string ProductTagline { get; } = "Desktop companion for React Native CLI project setup.";

    public string PageTitle { get; } = "Workspace";

    public string PageSubtitle { get; } =
        "Discover local mobile projects and reusable Fabricator templates from one workspace.";

    public string MilestoneLabel { get; } = $"v{ProductInfo.Version} workspace discovery";

    public string WorkspaceInputPath
    {
        get => _workspaceInputPath;
        set => SetProperty(ref _workspaceInputPath, value);
    }

    public string SelectedWorkspacePath
    {
        get => _selectedWorkspacePath;
        private set => SetProperty(ref _selectedWorkspacePath, value);
    }

    public string WorkspaceStatus
    {
        get => _workspaceStatus;
        private set => SetProperty(ref _workspaceStatus, value);
    }

    public string TemplateCatalogStatus
    {
        get => _templateCatalogStatus;
        private set => SetProperty(ref _templateCatalogStatus, value);
    }

    public bool HasWorkspace
    {
        get => _hasWorkspace;
        private set
        {
            if (SetProperty(ref _hasWorkspace, value))
            {
                OnPropertyChanged(nameof(IsCreateFormEnabled));
            }
        }
    }

    public bool HasTemplateCatalog
    {
        get => _hasTemplateCatalog;
        private set => SetProperty(ref _hasTemplateCatalog, value);
    }

    public IReadOnlyList<WorkspaceProjectItemViewModel> Projects
    {
        get => _projects;
        private set
        {
            if (SetProperty(ref _projects, value))
            {
                OnPropertyChanged(nameof(ProjectCountLabel));
                OnPropertyChanged(nameof(MissingFabricatorStateLabel));
                OnPropertyChanged(nameof(HasProjects));
                OnPropertyChanged(nameof(HasNoProjects));
                OnPropertyChanged(nameof(EmptyProjectListMessage));
            }
        }
    }

    public IReadOnlyList<TemplateCategoryGroupViewModel> TemplateGroups
    {
        get => _templateGroups;
        private set
        {
            if (SetProperty(ref _templateGroups, value))
            {
                OnPropertyChanged(nameof(HasTemplateGroups));
                OnPropertyChanged(nameof(HasNoTemplateGroups));
                OnPropertyChanged(nameof(TemplateGroupCountLabel));
            }
        }
    }

    public WorkspaceProjectItemViewModel? SelectedProject
    {
        get => _selectedProject;
        private set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                OnPropertyChanged(nameof(SelectedProjectLabel));
                OnPropertyChanged(nameof(HasSelectedProject));
            }
        }
    }

    public WorkspaceProjectDetailViewModel? SelectedProjectDetail
    {
        get => _selectedProjectDetail;
        private set => SetProperty(ref _selectedProjectDetail, value);
    }

    public bool HasProjects => Projects.Count > 0;

    public bool HasNoProjects => !HasProjects;

    public bool HasSelectedProject => SelectedProject is not null;

    public bool IsTemplatesView
    {
        get => _isTemplatesView;
        private set
        {
            if (SetProperty(ref _isTemplatesView, value))
            {
                OnPropertyChanged(nameof(IsWorkspaceView));
                OnPropertyChanged(nameof(IsCreateView));
                OnPropertyChanged(nameof(IsDoctorView));
                OnPropertyChanged(nameof(IsSetupView));
            }
        }
    }

    public bool IsCreateView
    {
        get => _isCreateView;
        private set
        {
            if (SetProperty(ref _isCreateView, value))
            {
                OnPropertyChanged(nameof(IsWorkspaceView));
                OnPropertyChanged(nameof(IsTemplatesView));
                OnPropertyChanged(nameof(IsDoctorView));
                OnPropertyChanged(nameof(IsSetupView));
            }
        }
    }

    public bool IsDoctorView
    {
        get => _isDoctorView;
        private set
        {
            if (SetProperty(ref _isDoctorView, value))
            {
                OnPropertyChanged(nameof(IsWorkspaceView));
                OnPropertyChanged(nameof(IsTemplatesView));
                OnPropertyChanged(nameof(IsCreateView));
                OnPropertyChanged(nameof(IsSetupView));
            }
        }
    }

    public bool IsSetupView
    {
        get => _isSetupView;
        private set
        {
            if (SetProperty(ref _isSetupView, value))
            {
                OnPropertyChanged(nameof(IsWorkspaceView));
                OnPropertyChanged(nameof(IsTemplatesView));
                OnPropertyChanged(nameof(IsCreateView));
                OnPropertyChanged(nameof(IsDoctorView));
            }
        }
    }

    public bool IsWorkspaceView => !IsTemplatesView && !IsCreateView && !IsDoctorView && !IsSetupView;

    public string CreateProjectName
    {
        get => _createProjectName;
        set
        {
            if (SetProperty(ref _createProjectName, value))
            {
                OnPropertyChanged(nameof(CreateTargetProjectPath));
            }
        }
    }

    public string CreateOutputDirectory
    {
        get => _createOutputDirectory;
        set
        {
            if (SetProperty(ref _createOutputDirectory, value))
            {
                OnPropertyChanged(nameof(CreateTargetProjectPath));
            }
        }
    }

    public string CreateTemplateName
    {
        get => _createTemplateName;
        set => SetProperty(ref _createTemplateName, value);
    }

    public string CreateTemplateSource
    {
        get => _createTemplateSource;
        set => SetProperty(ref _createTemplateSource, value);
    }

    public bool CreateInstallPods
    {
        get => _createInstallPods;
        set
        {
            if (SetProperty(ref _createInstallPods, value))
            {
                OnPropertyChanged(nameof(CreateInstallPodsLabel));
            }
        }
    }

    public string CreateInstallPodsLabel => CreateInstallPods
        ? "Install during create"
        : "Skip during create";

    public string CreateFormStatus
    {
        get => _createFormStatus;
        private set => SetProperty(ref _createFormStatus, value);
    }

    public bool IsCreateFormEnabled => HasWorkspace;

    public bool IsCreateReviewStep
    {
        get => _isCreateReviewStep;
        private set
        {
            if (SetProperty(ref _isCreateReviewStep, value))
            {
                OnPropertyChanged(nameof(IsCreateEditStep));
            }
        }
    }

    public bool IsCreateEditStep => !IsCreateReviewStep;

    public bool IsCreateConfirmed
    {
        get => _isCreateConfirmed;
        private set => SetProperty(ref _isCreateConfirmed, value);
    }

    public bool IsCreateRunning
    {
        get => _isCreateRunning;
        private set => SetProperty(ref _isCreateRunning, value);
    }

    public bool HasCreateRun
    {
        get => _hasCreateRun;
        private set => SetProperty(ref _hasCreateRun, value);
    }

    public bool IsCreateSucceeded
    {
        get => _isCreateSucceeded;
        private set => SetProperty(ref _isCreateSucceeded, value);
    }

    public bool IsCreateFailed
    {
        get => _isCreateFailed;
        private set => SetProperty(ref _isCreateFailed, value);
    }

    public string CreateExecutionState
    {
        get => _createExecutionState;
        private set => SetProperty(ref _createExecutionState, value);
    }

    public string CreatePreparedCommand
    {
        get => _createPreparedCommand;
        private set => SetProperty(ref _createPreparedCommand, value);
    }

    public string CreateStandardOutput
    {
        get => _createStandardOutput;
        private set => SetProperty(ref _createStandardOutput, value);
    }

    public string CreateStandardError
    {
        get => _createStandardError;
        private set => SetProperty(ref _createStandardError, value);
    }

    public string CreateResultSummary
    {
        get => _createResultSummary;
        private set => SetProperty(ref _createResultSummary, value);
    }

    public IReadOnlyList<DoctorCheckGroupViewModel> DoctorGroups
    {
        get => _doctorGroups;
        private set
        {
            if (SetProperty(ref _doctorGroups, value))
            {
                OnPropertyChanged(nameof(HasDoctorResults));
                OnPropertyChanged(nameof(HasNoDoctorResults));
            }
        }
    }

    public bool HasDoctorResults => DoctorGroups.Count > 0;

    public bool HasNoDoctorResults => !HasDoctorResults;

    public bool IsDoctorRunning
    {
        get => _isDoctorRunning;
        private set => SetProperty(ref _isDoctorRunning, value);
    }

    public bool HasDoctorRun
    {
        get => _hasDoctorRun;
        private set => SetProperty(ref _hasDoctorRun, value);
    }

    public string DoctorStatus
    {
        get => _doctorStatus;
        private set => SetProperty(ref _doctorStatus, value);
    }

    public string DoctorSummaryLabel
    {
        get => _doctorSummaryLabel;
        private set => SetProperty(ref _doctorSummaryLabel, value);
    }

    public IReadOnlyList<SetupPlanItemViewModel> SetupPlanItems
    {
        get => _setupPlanItems;
        private set
        {
            if (SetProperty(ref _setupPlanItems, value))
            {
                OnPropertyChanged(nameof(HasSetupPlanItems));
                OnPropertyChanged(nameof(HasNoSetupPlanItems));
            }
        }
    }

    public bool HasSetupPlanItems => SetupPlanItems.Count > 0;

    public bool HasNoSetupPlanItems => !HasSetupPlanItems;

    public bool IsSetupPlanRunning
    {
        get => _isSetupPlanRunning;
        private set => SetProperty(ref _isSetupPlanRunning, value);
    }

    public bool HasSetupPlanRun
    {
        get => _hasSetupPlanRun;
        private set => SetProperty(ref _hasSetupPlanRun, value);
    }

    public string SetupPlanStatus
    {
        get => _setupPlanStatus;
        private set => SetProperty(ref _setupPlanStatus, value);
    }

    public string SetupPlanSummaryLabel
    {
        get => _setupPlanSummaryLabel;
        private set => SetProperty(ref _setupPlanSummaryLabel, value);
    }

    public string SetupPlanPlatformLabel
    {
        get => _setupPlanPlatformLabel;
        private set => SetProperty(ref _setupPlanPlatformLabel, value);
    }

    public string SetupPlanPackageManagerLabel
    {
        get => _setupPlanPackageManagerLabel;
        private set => SetProperty(ref _setupPlanPackageManagerLabel, value);
    }

    public string SetupPlanToolchainLabel
    {
        get => _setupPlanToolchainLabel;
        private set => SetProperty(ref _setupPlanToolchainLabel, value);
    }

    public string CreateTargetProjectPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CreateOutputDirectory) || string.IsNullOrWhiteSpace(CreateProjectName))
            {
                return "Project target is not ready.";
            }

            try
            {
                return Path.GetFullPath(Path.Combine(CreateOutputDirectory, CreateProjectName));
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return "Project target path is invalid.";
            }
        }
    }

    public bool HasTemplateGroups => TemplateGroups.Count > 0;

    public bool HasNoTemplateGroups => !HasTemplateGroups;

    public string TemplateGroupCountLabel => $"{TemplateGroups.Count} categor(ies)";

    public string ProjectCountLabel => $"{Projects.Count} project(s)";

    public string MissingFabricatorStateLabel =>
        $"{Projects.Count(project => !project.HasFabricatorState)} missing Fabricator state";

    public string EmptyProjectListMessage => HasWorkspace
        ? "No mobile projects found in this workspace."
        : "Load a workspace to discover mobile projects.";

    public string SelectedProjectLabel => SelectedProject is null
        ? "No project selected"
        : $"Selected: {SelectedProject.Name}";

    public IRelayCommand LoadWorkspaceCommand { get; }

    public IRelayCommand<WorkspaceProjectItemViewModel> SelectProjectCommand { get; }

    public IRelayCommand ClearProjectSelectionCommand { get; }

    public IRelayCommand ShowWorkspaceCommand { get; }

    public IRelayCommand ShowTemplatesCommand { get; }

    public IRelayCommand ShowCreateCommand { get; }

    public IAsyncRelayCommand ShowDoctorCommand { get; }

    public IAsyncRelayCommand RunDoctorCommand { get; }

    public IAsyncRelayCommand ShowSetupCommand { get; }

    public IAsyncRelayCommand RunSetupPlanCommand { get; }

    public IRelayCommand ReviewCreateCommand { get; }

    public IRelayCommand EditCreateCommand { get; }

    public IAsyncRelayCommand ConfirmCreateCommand { get; }

    public IReadOnlyList<ShellNavigationItem> NavigationItems { get; } =
    [
        new("Workspace", "Local project discovery."),
        new("Templates", "Grouped reusable template catalog."),
        new("Doctor", "Environment diagnostics."),
        new("Setup", "Guided dependency planning.")
    ];

    private void LoadWorkspaceFromInput()
    {
        LoadWorkspace(WorkspaceInputPath, save: true);
    }

    private void LoadWorkspace(string workspacePath, bool save)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            HasWorkspace = false;
            HasTemplateCatalog = false;
            SelectedWorkspacePath = "No workspace selected";
            WorkspaceStatus = "Select a workspace directory to begin.";
            TemplateCatalogStatus = "No workspace loaded.";
            Projects = [];
            SelectedProject = null;
            SelectedProjectDetail = null;
            ResetCreateFormDefaults(resultWorkspacePath: string.Empty, templateCatalogPath: string.Empty, workspaceExists: false, templateCatalogExists: false);
            return;
        }

        ApplyWorkspaceDiscoveryResult(
            _workspaceDiscoveryService.Discover(workspacePath),
            save,
            resetCreateForm: true);
    }

    private void ApplyWorkspaceDiscoveryResult(
        WorkspaceDiscoveryResult result,
        bool save,
        bool resetCreateForm)
    {
        SelectedWorkspacePath = result.WorkspacePath;
        HasWorkspace = result.WorkspaceExists;
        HasTemplateCatalog = result.TemplateCatalog.Exists;
        TemplateGroups = result.TemplateCatalog.Exists
            ? LoadTemplateGroups(result.TemplateCatalog.Path)
            : [];
        TemplateCatalogStatus = result.TemplateCatalog.Exists
            ? $"Templates catalog found: {result.TemplateCatalog.Path}"
            : $"Templates catalog missing: {result.TemplateCatalog.Path}";
        Projects = result.Projects
            .Select(candidate => new WorkspaceProjectItemViewModel(candidate))
            .ToArray();
        SelectedProject = null;
        SelectedProjectDetail = null;

        if (resetCreateForm)
        {
            IsTemplatesView = false;
            IsCreateView = false;
            IsDoctorView = false;
            IsSetupView = false;
        }

        WorkspaceStatus = result.WorkspaceExists
            ? $"Workspace loaded from {result.WorkspacePath}"
            : $"Workspace directory was not found: {result.WorkspacePath}";

        if (resetCreateForm)
        {
            ResetCreateFormDefaults(
                result.WorkspacePath,
                result.TemplateCatalog.Path,
                result.WorkspaceExists,
                result.TemplateCatalog.Exists);
        }

        if (save && result.WorkspaceExists)
        {
            _workspaceSettingsStore.SaveLastWorkspacePath(result.WorkspacePath);
        }
    }

    private void SelectProject(WorkspaceProjectItemViewModel? project)
    {
        SelectedProject = project;
        SelectedProjectDetail = project is null
            ? null
            : new WorkspaceProjectDetailViewModel(_workspaceProjectDetailService.GetDetail(project.Path));
    }

    private void RefreshWorkspaceAfterCreate(CreateProjectResult result)
    {
        if (!HasWorkspace || !Directory.Exists(SelectedWorkspacePath))
        {
            return;
        }

        ApplyWorkspaceDiscoveryResult(
            _workspaceDiscoveryService.Discover(SelectedWorkspacePath),
            save: false,
            resetCreateForm: false);
        SelectCreatedProject(result.ProjectPath);
        WorkspaceStatus = $"Workspace refreshed after creating {Path.GetFileName(result.ProjectPath)}.";
    }

    private void SelectCreatedProject(string projectPath)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        var project = Projects.FirstOrDefault(candidate =>
            string.Equals(Path.GetFullPath(candidate.Path), fullProjectPath, StringComparison.OrdinalIgnoreCase));

        if (project is not null)
        {
            SelectProject(project);
        }
    }

    private void ShowWorkspace()
    {
        IsTemplatesView = false;
        IsCreateView = false;
        IsDoctorView = false;
        IsSetupView = false;
    }

    private void ShowTemplates()
    {
        IsCreateView = false;
        IsDoctorView = false;
        IsSetupView = false;
        IsTemplatesView = true;
    }

    private void ShowCreate()
    {
        IsTemplatesView = false;
        IsDoctorView = false;
        IsSetupView = false;
        IsCreateView = true;

        if (!HasWorkspace)
        {
            CreateFormStatus = "Load an existing workspace before creating a project.";
        }
    }

    private async Task ShowDoctorAsync()
    {
        IsTemplatesView = false;
        IsCreateView = false;
        IsSetupView = false;
        IsDoctorView = true;

        if (!HasDoctorRun)
        {
            await RunDoctorAsync();
        }
    }

    private async Task ShowSetupAsync()
    {
        IsTemplatesView = false;
        IsCreateView = false;
        IsDoctorView = false;
        IsSetupView = true;

        if (!HasSetupPlanRun)
        {
            await RunSetupPlanAsync();
        }
    }

    private async Task RunSetupPlanAsync()
    {
        if (IsSetupPlanRunning)
        {
            return;
        }

        IsSetupPlanRunning = true;
        HasSetupPlanRun = true;
        SetupPlanStatus = "Building setup plan from local environment checks.";
        SetupPlanSummaryLabel = "Setup plan is running.";
        SetupPlanItems = [];

        try
        {
            var plan = await _setupPlanService.BuildPlanAsync(SetupPlanRequest.Default);
            SetupPlanPlatformLabel = $"Platform: {plan.PlatformName}";
            SetupPlanPackageManagerLabel = $"Package manager: {FormatPackageManager(plan.PackageManager)}";
            SetupPlanToolchainLabel = plan.ToolchainProfile is null
                ? "Toolchain profile: default"
                : $"Toolchain profile: {plan.ToolchainProfile.DisplayName} ({plan.ToolchainProfile.ReactNativeVersion})";
            SetupPlanSummaryLabel = $"Commands {plan.CommandCount}, manual {plan.ManualCount}, environment {plan.EnvironmentCount}.";
            SetupPlanStatus = plan.HasItems
                ? "Review the setup plan before running any install commands."
                : "No setup actions needed. Environment checks passed.";
            SetupPlanItems = plan.Items
                .Select(item => new SetupPlanItemViewModel(item))
                .ToArray();
        }
        catch (SetupPlanException exception)
        {
            SetupPlanStatus = $"Setup plan input failed: {string.Join(" ", exception.Errors)}";
            SetupPlanSummaryLabel = "Setup plan failed.";
            SetupPlanItems = [];
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetupPlanStatus = $"Setup plan failed unexpectedly: {exception.Message}";
            SetupPlanSummaryLabel = "Setup plan failed.";
            SetupPlanItems = [];
        }
        finally
        {
            IsSetupPlanRunning = false;
        }
    }

    private async Task RunDoctorAsync()
    {
        if (IsDoctorRunning)
        {
            return;
        }

        IsDoctorRunning = true;
        HasDoctorRun = true;
        DoctorStatus = "Checking local React Native development tools.";
        DoctorSummaryLabel = "Checks are running.";
        DoctorGroups = [];

        try
        {
            var coreSummary = await _dependencyCheckService.CheckCoreToolsAsync();
            var appleSummary = await _dependencyCheckService.CheckAppleToolsAsync();
            var androidSummary = await _dependencyCheckService.CheckAndroidToolsAsync();
            DoctorGroups =
            [
                new DoctorCheckGroupViewModel("Core tools", coreSummary),
                new DoctorCheckGroupViewModel("Apple tools", appleSummary),
                new DoctorCheckGroupViewModel("Android tools", androidSummary)
            ];

            var combined = new DependencyCheckSummary(DoctorGroups.SelectMany(group => group.Results).Select(item => item.Result).ToArray());
            DoctorSummaryLabel = $"Passed {combined.PassedCount}, warnings {combined.WarningCount}, failed {combined.FailedCount}.";
            DoctorStatus = combined.HasRequiredFailures
                ? "Doctor found required tools that need attention."
                : "Doctor checks completed.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            DoctorStatus = $"Doctor failed unexpectedly: {exception.Message}";
            DoctorSummaryLabel = "Checks failed.";
            DoctorGroups = [];
        }
        finally
        {
            IsDoctorRunning = false;
        }
    }

    private void ReviewCreate()
    {
        if (!HasWorkspace)
        {
            CreateFormStatus = "Load an existing workspace before reviewing create inputs.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CreateProjectName))
        {
            CreateFormStatus = "Project name is required before review.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CreateOutputDirectory))
        {
            CreateFormStatus = "Output directory is required before review.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CreateTemplateName))
        {
            CreateFormStatus = "Starter template is required before review.";
            return;
        }

        IsCreateConfirmed = false;
        IsCreateReviewStep = true;
        CreateFormStatus = "Review the target path and create inputs before confirmation.";
    }

    private void EditCreate()
    {
        IsCreateConfirmed = false;
        IsCreateReviewStep = false;
        CreateFormStatus = HasWorkspace
            ? "Ready to edit create inputs."
            : "Load an existing workspace before creating a project.";
    }

    private async Task ConfirmCreateAsync()
    {
        if (!IsCreateReviewStep || IsCreateRunning)
        {
            return;
        }

        IsCreateConfirmed = true;
        await RunCreateAsync();
    }

    private async Task RunCreateAsync()
    {
        ResetCreateExecutionState();
        IsCreateRunning = true;
        CreateExecutionState = "Running";
        CreateFormStatus = "Create is running.";

        var request = new CreateProjectRequest(
            CreateProjectName,
            CreateTemplateName,
            CreateOutputDirectory,
            OnCommandPrepared: command => CreatePreparedCommand = FormatCommand(command),
            OnStandardOutput: chunk => CreateStandardOutput += chunk,
            OnStandardError: chunk => CreateStandardError += chunk,
            TemplateSource: string.IsNullOrWhiteSpace(CreateTemplateSource) ? null : CreateTemplateSource,
            InstallPods: CreateInstallPods);

        try
        {
            var result = await _createProjectService.CreateAsync(request);
            HasCreateRun = true;
            IsCreateSucceeded = result.Succeeded;
            IsCreateFailed = !result.Succeeded;
            CreateExecutionState = result.Succeeded ? "Succeeded" : "Failed";
            CreateResultSummary = BuildCreateResultSummary(result);
            CreateFormStatus = result.Succeeded
                ? "Create completed successfully."
                : "Create failed. Review the result and logs.";

            if (result.Succeeded)
            {
                RefreshWorkspaceAfterCreate(result);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            HasCreateRun = true;
            IsCreateSucceeded = false;
            IsCreateFailed = true;
            CreateExecutionState = "Failed";
            CreateResultSummary = $"Create failed unexpectedly: {exception.Message}";
            CreateFormStatus = "Create failed. Review the result and logs.";
        }
        finally
        {
            IsCreateRunning = false;
        }
    }

    private void ResetCreateFormDefaults(
        string resultWorkspacePath,
        string templateCatalogPath,
        bool workspaceExists,
        bool templateCatalogExists)
    {
        IsCreateConfirmed = false;
        IsCreateReviewStep = false;
        ResetCreateExecutionState();
        CreateProjectName = string.Empty;
        CreateOutputDirectory = workspaceExists ? resultWorkspacePath : string.Empty;
        CreateTemplateName = CreateProjectService.DefaultStarterId;
        CreateTemplateSource = templateCatalogExists ? templateCatalogPath : string.Empty;
        CreateInstallPods = false;
        CreateFormStatus = workspaceExists
            ? templateCatalogExists
                ? "Ready to configure a new project. The workspace template catalog is selected."
                : "Ready to configure a new project. Add a template source or use the embedded starter."
            : "Load an existing workspace before creating a project.";
    }

    private void ResetCreateExecutionState()
    {
        IsCreateRunning = false;
        HasCreateRun = false;
        IsCreateSucceeded = false;
        IsCreateFailed = false;
        CreateExecutionState = "Idle";
        CreatePreparedCommand = "No command prepared.";
        CreateStandardOutput = string.Empty;
        CreateStandardError = string.Empty;
        CreateResultSummary = "Create has not run.";
    }

    private static ICreateProjectService CreateDefaultCreateProjectService()
    {
        return new CreateProjectService(new CreateProjectValidator(), new ProcessRunner());
    }

    private static IDependencyCheckService CreateDefaultDependencyCheckService()
    {
        return new DependencyCheckService(new ProcessRunner());
    }

    private static ISetupPlanService CreateDefaultSetupPlanService()
    {
        var processRunner = new ProcessRunner();
        var systemPlatform = new SystemPlatform();

        return new SetupPlanService(
            new DependencyCheckService(processRunner, systemPlatform),
            systemPlatform,
            new PackageManagerDetector(processRunner, systemPlatform),
            new LocalToolchainProfileProvider());
    }

    private static string FormatPackageManager(PackageManagerInfo packageManager)
    {
        return packageManager.IsAvailable && !string.IsNullOrWhiteSpace(packageManager.CommandName)
            ? $"{packageManager.Name} ({packageManager.CommandName})"
            : "not detected; manual guidance will be used";
    }

    private static string FormatCommand(ProcessRunRequest command)
    {
        var parts = new List<string> { command.FileName };
        parts.AddRange(command.Arguments);

        if (!string.IsNullOrWhiteSpace(command.WorkingDirectory))
        {
            parts.Add($"--working-directory={command.WorkingDirectory}");
        }

        return string.Join(" ", parts);
    }

    private static string BuildCreateResultSummary(CreateProjectResult result)
    {
        if (!result.Validation.IsValid)
        {
            return $"Validation failed: {string.Join(" ", result.Validation.Errors)} {BuildRollbackSummary(result.Rollback)}";
        }

        if (result.ProcessResult?.Succeeded != true)
        {
            var details = new StringBuilder($"Process failed with exit code {result.ExitCode}.");

            if (!string.IsNullOrWhiteSpace(result.ProcessResult?.StandardError))
            {
                details.Append(' ');
                details.Append(result.ProcessResult.StandardError.Trim());
            }

            details.Append(' ');
            details.Append(BuildRollbackSummary(result.Rollback));

            return details.ToString();
        }

        if (result.StarterResult?.Succeeded == false)
        {
            return $"Starter failed: {string.Join(" ", result.StarterResult.Errors)} {BuildRollbackSummary(result.Rollback)}";
        }

        return result.StarterResult is null
            ? $"Created project at {result.ProjectPath}. Starter: not applied."
            : $"Created project at {result.ProjectPath}. Starter: {result.StarterResult.StarterId} ({result.StarterResult.GeneratedFiles.Count} file(s)).";
    }

    private static string BuildRollbackSummary(CreateProjectRollbackResult rollback)
    {
        if (string.IsNullOrWhiteSpace(rollback.ErrorMessage))
        {
            return $"Rollback: {rollback.Message}";
        }

        return $"Rollback: {rollback.Message} {rollback.ErrorMessage}";
    }

    private static IReadOnlyList<TemplateCategoryGroupViewModel> LoadTemplateGroups(string catalogPath)
    {
        try
        {
            var catalog = JsonSerializer.Deserialize<FabricatorTemplateCatalog>(
                File.ReadAllText(catalogPath),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (catalog is null)
            {
                return [];
            }

            return catalog.Templates
                .GroupBy(
                    template => string.IsNullOrWhiteSpace(template.Category)
                        ? "uncategorized"
                        : template.Category,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new TemplateCategoryGroupViewModel(
                    group.Key,
                    group
                        .OrderBy(template => template.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .Select(template => new TemplateCatalogItemViewModel(template))
                        .ToArray()))
                .ToArray();
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return [];
        }
    }
}

public sealed class DoctorCheckGroupViewModel
{
    public DoctorCheckGroupViewModel(string title, DependencyCheckSummary summary)
    {
        Title = title;
        Results = summary.Results
            .Select(result => new DoctorCheckItemViewModel(result))
            .ToArray();
        Summary = $"Passed {summary.PassedCount}, warnings {summary.WarningCount}, failed {summary.FailedCount}.";
    }

    public string Title { get; }

    public string Summary { get; }

    public IReadOnlyList<DoctorCheckItemViewModel> Results { get; }
}

public sealed class DoctorCheckItemViewModel
{
    public DoctorCheckItemViewModel(DependencyCheckResult result)
    {
        Result = result;
    }

    public DependencyCheckResult Result { get; }

    public string Name => Result.Name;

    public string StatusLabel => Result.Status switch
    {
        DependencyCheckStatus.Passed => "PASS",
        DependencyCheckStatus.Warning => "WARN",
        DependencyCheckStatus.Failed => "FAIL",
        _ => "UNKNOWN"
    };

    public string VersionLabel => string.IsNullOrWhiteSpace(Result.DetectedVersion)
        ? "Version not detected"
        : Result.DetectedVersion;

    public string Message => Result.Message;

    public string RemediationHint => Result.RemediationHint ?? string.Empty;

    public bool HasRemediationHint => !string.IsNullOrWhiteSpace(Result.RemediationHint);
}

public sealed class SetupPlanItemViewModel
{
    public SetupPlanItemViewModel(SetupPlanItem item)
    {
        DependencyName = item.DependencyName;
        KindLabel = item.Kind switch
        {
            SetupPlanItemKind.Command => "COMMAND",
            SetupPlanItemKind.Environment => "ENV",
            SetupPlanItemKind.Manual => "MANUAL",
            _ => "UNKNOWN"
        };
        Title = item.RequiresAdmin
            ? $"{item.Title} (may require admin privileges)"
            : item.Title;
        Steps = item.Steps;
    }

    public string DependencyName { get; }

    public string KindLabel { get; }

    public string Title { get; }

    public IReadOnlyList<string> Steps { get; }
}
