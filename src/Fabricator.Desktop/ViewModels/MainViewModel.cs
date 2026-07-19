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
    private readonly IApplicationCommandCenterMetadataService _commandCenterMetadataService;
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
    private string _commandCenterCreateStatus = "Select a project to manage command center metadata.";
    private string _publishingEditStatus = "Select a project with command center metadata to edit publishing.";
    private string _publishingIosDisplayName = string.Empty;
    private string _publishingIosIdentifier = string.Empty;
    private string _publishingIosVersion = string.Empty;
    private string _publishingIosBuildNumber = string.Empty;
    private string _publishingIosStatus = string.Empty;
    private string _publishingIosStoreUrl = string.Empty;
    private string _publishingIosNotes = string.Empty;
    private string _publishingAndroidDisplayName = string.Empty;
    private string _publishingAndroidIdentifier = string.Empty;
    private string _publishingAndroidVersion = string.Empty;
    private string _publishingAndroidBuildNumber = string.Empty;
    private string _publishingAndroidStatus = string.Empty;
    private string _publishingAndroidStoreUrl = string.Empty;
    private string _publishingAndroidNotes = string.Empty;
    private string _publishingReleaseOwner = string.Empty;
    private string _publishingNotes = string.Empty;
    private string _researchEditStatus = "Select a project with command center metadata to edit research.";
    private string _researchTargetAudience = string.Empty;
    private string _researchPositioning = string.Empty;
    private string _researchKeywordsText = string.Empty;
    private string _researchCompetitorsText = string.Empty;
    private string _researchOpenQuestionsText = string.Empty;
    private string _researchGrowthAssumptionsText = string.Empty;
    private string _researchNotes = string.Empty;
    private string _projectMetricsEditStatus = "Select a project with command center metadata to edit project metrics.";
    private string _projectTargetUsers = string.Empty;
    private string _projectTargetDate = string.Empty;
    private string _projectReportingCadence = string.Empty;
    private string _projectMetricSnapshotsText = string.Empty;
    private string _projectMilestoneProgressText = string.Empty;
    private string _projectIntelligenceNotes = string.Empty;

    public MainViewModel()
        : this(
            new WorkspaceDiscoveryService(),
            new WorkspaceProjectDetailService(),
            new FileWorkspaceSettingsStore(),
            CreateDefaultCreateProjectService(),
            CreateDefaultDependencyCheckService(),
            CreateDefaultSetupPlanService(),
            new ApplicationCommandCenterMetadataService())
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
            CreateDefaultSetupPlanService(),
            new ApplicationCommandCenterMetadataService())
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
            CreateDefaultSetupPlanService(),
            new ApplicationCommandCenterMetadataService())
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
            CreateDefaultSetupPlanService(),
            new ApplicationCommandCenterMetadataService())
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
            CreateDefaultSetupPlanService(),
            new ApplicationCommandCenterMetadataService())
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceProjectDetailService workspaceProjectDetailService,
        IWorkspaceSettingsStore workspaceSettingsStore,
        ICreateProjectService createProjectService,
        IDependencyCheckService dependencyCheckService,
        ISetupPlanService setupPlanService,
        IApplicationCommandCenterMetadataService commandCenterMetadataService)
    {
        _workspaceDiscoveryService = workspaceDiscoveryService;
        _workspaceProjectDetailService = workspaceProjectDetailService;
        _workspaceSettingsStore = workspaceSettingsStore;
        _createProjectService = createProjectService;
        _dependencyCheckService = dependencyCheckService;
        _setupPlanService = setupPlanService;
        _commandCenterMetadataService = commandCenterMetadataService;
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
        CreateCommandCenterMetadataCommand = new RelayCommand(CreateCommandCenterMetadata);
        SavePublishingMetadataCommand = new RelayCommand(SavePublishingMetadata);
        SaveResearchMetadataCommand = new RelayCommand(SaveResearchMetadata);
        SaveProjectMetricsCommand = new RelayCommand(SaveProjectMetrics);

        var lastWorkspacePath = _workspaceSettingsStore.LoadLastWorkspacePath();
        if (!string.IsNullOrWhiteSpace(lastWorkspacePath))
        {
            WorkspaceInputPath = lastWorkspacePath;
            LoadWorkspace(lastWorkspacePath, save: false);
        }
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceProjectDetailService workspaceProjectDetailService,
        IWorkspaceSettingsStore workspaceSettingsStore,
        ICreateProjectService createProjectService,
        IDependencyCheckService dependencyCheckService,
        ISetupPlanService setupPlanService)
        : this(
            workspaceDiscoveryService,
            workspaceProjectDetailService,
            workspaceSettingsStore,
            createProjectService,
            dependencyCheckService,
            setupPlanService,
            new ApplicationCommandCenterMetadataService())
    {
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
        private set
        {
            if (SetProperty(ref _isDoctorRunning, value))
            {
                OnPropertyChanged(nameof(CreateReadinessStatus));
                OnPropertyChanged(nameof(CreateReadinessActionLabel));
            }
        }
    }

    public bool HasDoctorRun
    {
        get => _hasDoctorRun;
        private set
        {
            if (SetProperty(ref _hasDoctorRun, value))
            {
                OnPropertyChanged(nameof(CreateReadinessStatus));
                OnPropertyChanged(nameof(CreateReadinessActionLabel));
            }
        }
    }

    public string DoctorStatus
    {
        get => _doctorStatus;
        private set
        {
            if (SetProperty(ref _doctorStatus, value))
            {
                OnPropertyChanged(nameof(CreateReadinessStatus));
            }
        }
    }

    public string DoctorSummaryLabel
    {
        get => _doctorSummaryLabel;
        private set
        {
            if (SetProperty(ref _doctorSummaryLabel, value))
            {
                OnPropertyChanged(nameof(CreateReadinessStatus));
            }
        }
    }

    public string CreateReadinessStatus
    {
        get
        {
            if (IsDoctorRunning)
            {
                return "Doctor is checking the local React Native toolchain.";
            }

            if (!HasDoctorRun)
            {
                return "Doctor has not run for this session. Run Doctor before creating when you need environment confidence.";
            }

            return $"{DoctorStatus} {DoctorSummaryLabel}";
        }
    }

    public string CreateReadinessActionLabel => IsDoctorRunning
        ? "Doctor running"
        : HasDoctorRun
            ? "Run Doctor again"
            : "Run Doctor";

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

    public string CommandCenterCreateStatus
    {
        get => _commandCenterCreateStatus;
        private set => SetProperty(ref _commandCenterCreateStatus, value);
    }

    public string PublishingEditStatus
    {
        get => _publishingEditStatus;
        private set => SetProperty(ref _publishingEditStatus, value);
    }

    public string PublishingIosDisplayName
    {
        get => _publishingIosDisplayName;
        set => SetProperty(ref _publishingIosDisplayName, value);
    }

    public string PublishingIosIdentifier
    {
        get => _publishingIosIdentifier;
        set => SetProperty(ref _publishingIosIdentifier, value);
    }

    public string PublishingIosVersion
    {
        get => _publishingIosVersion;
        set => SetProperty(ref _publishingIosVersion, value);
    }

    public string PublishingIosBuildNumber
    {
        get => _publishingIosBuildNumber;
        set => SetProperty(ref _publishingIosBuildNumber, value);
    }

    public string PublishingIosStatus
    {
        get => _publishingIosStatus;
        set => SetProperty(ref _publishingIosStatus, value);
    }

    public string PublishingIosStoreUrl
    {
        get => _publishingIosStoreUrl;
        set => SetProperty(ref _publishingIosStoreUrl, value);
    }

    public string PublishingIosNotes
    {
        get => _publishingIosNotes;
        set => SetProperty(ref _publishingIosNotes, value);
    }

    public string PublishingAndroidDisplayName
    {
        get => _publishingAndroidDisplayName;
        set => SetProperty(ref _publishingAndroidDisplayName, value);
    }

    public string PublishingAndroidIdentifier
    {
        get => _publishingAndroidIdentifier;
        set => SetProperty(ref _publishingAndroidIdentifier, value);
    }

    public string PublishingAndroidVersion
    {
        get => _publishingAndroidVersion;
        set => SetProperty(ref _publishingAndroidVersion, value);
    }

    public string PublishingAndroidBuildNumber
    {
        get => _publishingAndroidBuildNumber;
        set => SetProperty(ref _publishingAndroidBuildNumber, value);
    }

    public string PublishingAndroidStatus
    {
        get => _publishingAndroidStatus;
        set => SetProperty(ref _publishingAndroidStatus, value);
    }

    public string PublishingAndroidStoreUrl
    {
        get => _publishingAndroidStoreUrl;
        set => SetProperty(ref _publishingAndroidStoreUrl, value);
    }

    public string PublishingAndroidNotes
    {
        get => _publishingAndroidNotes;
        set => SetProperty(ref _publishingAndroidNotes, value);
    }

    public string PublishingReleaseOwner
    {
        get => _publishingReleaseOwner;
        set => SetProperty(ref _publishingReleaseOwner, value);
    }

    public string PublishingNotes
    {
        get => _publishingNotes;
        set => SetProperty(ref _publishingNotes, value);
    }

    public string ResearchEditStatus
    {
        get => _researchEditStatus;
        private set => SetProperty(ref _researchEditStatus, value);
    }

    public string ResearchTargetAudience
    {
        get => _researchTargetAudience;
        set => SetProperty(ref _researchTargetAudience, value);
    }

    public string ResearchPositioning
    {
        get => _researchPositioning;
        set => SetProperty(ref _researchPositioning, value);
    }

    public string ResearchKeywordsText
    {
        get => _researchKeywordsText;
        set => SetProperty(ref _researchKeywordsText, value);
    }

    public string ResearchCompetitorsText
    {
        get => _researchCompetitorsText;
        set => SetProperty(ref _researchCompetitorsText, value);
    }

    public string ResearchOpenQuestionsText
    {
        get => _researchOpenQuestionsText;
        set => SetProperty(ref _researchOpenQuestionsText, value);
    }

    public string ResearchGrowthAssumptionsText
    {
        get => _researchGrowthAssumptionsText;
        set => SetProperty(ref _researchGrowthAssumptionsText, value);
    }

    public string ResearchNotes
    {
        get => _researchNotes;
        set => SetProperty(ref _researchNotes, value);
    }

    public string ProjectMetricsEditStatus
    {
        get => _projectMetricsEditStatus;
        private set => SetProperty(ref _projectMetricsEditStatus, value);
    }

    public string ProjectTargetUsers
    {
        get => _projectTargetUsers;
        set => SetProperty(ref _projectTargetUsers, value);
    }

    public string ProjectTargetDate
    {
        get => _projectTargetDate;
        set => SetProperty(ref _projectTargetDate, value);
    }

    public string ProjectReportingCadence
    {
        get => _projectReportingCadence;
        set => SetProperty(ref _projectReportingCadence, value);
    }

    public string ProjectMetricSnapshotsText
    {
        get => _projectMetricSnapshotsText;
        set => SetProperty(ref _projectMetricSnapshotsText, value);
    }

    public string ProjectMilestoneProgressText
    {
        get => _projectMilestoneProgressText;
        set => SetProperty(ref _projectMilestoneProgressText, value);
    }

    public string ProjectIntelligenceNotes
    {
        get => _projectIntelligenceNotes;
        set => SetProperty(ref _projectIntelligenceNotes, value);
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

    public IRelayCommand CreateCommandCenterMetadataCommand { get; }

    public IRelayCommand SavePublishingMetadataCommand { get; }

    public IRelayCommand SaveResearchMetadataCommand { get; }

    public IRelayCommand SaveProjectMetricsCommand { get; }

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
        CommandCenterCreateStatus = SelectedProjectDetail is null
            ? "Select a project to manage command center metadata."
            : SelectedProjectDetail.CanCreateCommandCenterMetadata
                ? "Command center metadata is missing. Create local metadata before editing project work."
                : "Command center metadata is available for this project.";
        LoadPublishingEditor(project);
        LoadResearchEditor(project);
        LoadProjectMetricsEditor(project);
    }

    private void CreateCommandCenterMetadata()
    {
        if (SelectedProject is null)
        {
            CommandCenterCreateStatus = "Select a project before creating command center metadata.";
            return;
        }

        var result = _commandCenterMetadataService.Write(
            SelectedProject.Path,
            ApplicationCommandCenterMetadata.Empty);

        if (!result.IsSuccess)
        {
            CommandCenterCreateStatus = $"Command center metadata could not be created: {string.Join(" ", result.Errors)}";
            return;
        }

        SelectedProjectDetail = new WorkspaceProjectDetailViewModel(
            _workspaceProjectDetailService.GetDetail(SelectedProject.Path));
        CommandCenterCreateStatus = $"Command center metadata created: {result.MetadataPath}";
        LoadPublishingEditor(SelectedProject);
        LoadResearchEditor(SelectedProject);
        LoadProjectMetricsEditor(SelectedProject);
    }

    private void LoadPublishingEditor(WorkspaceProjectItemViewModel? project)
    {
        if (project is null)
        {
            PublishingEditStatus = "Select a project with command center metadata to edit publishing.";
            SetPublishingEditor(new ApplicationPublishingMetadata());
            return;
        }

        var readResult = _commandCenterMetadataService.Read(project.Path);
        if (!readResult.HasMetadata)
        {
            PublishingEditStatus = "Create command center metadata before editing publishing.";
            SetPublishingEditor(new ApplicationPublishingMetadata());
            return;
        }

        SetPublishingEditor(readResult.Metadata.Publishing);
        PublishingEditStatus = "Publishing metadata loaded for local editing.";
    }

    private void SetPublishingEditor(ApplicationPublishingMetadata publishing)
    {
        var ios = publishing.Ios ?? new ApplicationPlatformPublishingMetadata();
        var android = publishing.Android ?? new ApplicationPlatformPublishingMetadata();

        PublishingIosDisplayName = ios.DisplayName ?? string.Empty;
        PublishingIosIdentifier = ios.Identifier ?? string.Empty;
        PublishingIosVersion = ios.Version ?? string.Empty;
        PublishingIosBuildNumber = ios.BuildNumber ?? string.Empty;
        PublishingIosStatus = ios.Status ?? string.Empty;
        PublishingIosStoreUrl = ios.StoreUrl ?? string.Empty;
        PublishingIosNotes = ios.Notes ?? string.Empty;
        PublishingAndroidDisplayName = android.DisplayName ?? string.Empty;
        PublishingAndroidIdentifier = android.Identifier ?? string.Empty;
        PublishingAndroidVersion = android.Version ?? string.Empty;
        PublishingAndroidBuildNumber = android.BuildNumber ?? string.Empty;
        PublishingAndroidStatus = android.Status ?? string.Empty;
        PublishingAndroidStoreUrl = android.StoreUrl ?? string.Empty;
        PublishingAndroidNotes = android.Notes ?? string.Empty;
        PublishingReleaseOwner = publishing.ReleaseOwner ?? string.Empty;
        PublishingNotes = publishing.Notes ?? string.Empty;
    }

    private void SavePublishingMetadata()
    {
        if (SelectedProject is null)
        {
            PublishingEditStatus = "Select a project before saving publishing metadata.";
            return;
        }

        var readResult = _commandCenterMetadataService.Read(SelectedProject.Path);
        if (!readResult.HasMetadata)
        {
            PublishingEditStatus = "Create command center metadata before saving publishing.";
            return;
        }

        var metadata = readResult.Metadata with
        {
            Publishing = new ApplicationPublishingMetadata
            {
                Ios = new ApplicationPlatformPublishingMetadata
                {
                    DisplayName = Optional(PublishingIosDisplayName),
                    Identifier = Optional(PublishingIosIdentifier),
                    Version = Optional(PublishingIosVersion),
                    BuildNumber = Optional(PublishingIosBuildNumber),
                    Status = Optional(PublishingIosStatus),
                    StoreUrl = Optional(PublishingIosStoreUrl),
                    Notes = Optional(PublishingIosNotes)
                },
                Android = new ApplicationPlatformPublishingMetadata
                {
                    DisplayName = Optional(PublishingAndroidDisplayName),
                    Identifier = Optional(PublishingAndroidIdentifier),
                    Version = Optional(PublishingAndroidVersion),
                    BuildNumber = Optional(PublishingAndroidBuildNumber),
                    Status = Optional(PublishingAndroidStatus),
                    StoreUrl = Optional(PublishingAndroidStoreUrl),
                    Notes = Optional(PublishingAndroidNotes)
                },
                ReleaseOwner = Optional(PublishingReleaseOwner),
                Notes = Optional(PublishingNotes)
            }
        };

        var writeResult = _commandCenterMetadataService.Write(SelectedProject.Path, metadata);
        if (!writeResult.IsSuccess)
        {
            PublishingEditStatus = $"Publishing metadata could not be saved: {string.Join(" ", writeResult.Errors)}";
            return;
        }

        SelectedProjectDetail = new WorkspaceProjectDetailViewModel(
            _workspaceProjectDetailService.GetDetail(SelectedProject.Path));
        SetPublishingEditor(writeResult.Metadata.Publishing);
        PublishingEditStatus = $"Publishing metadata saved: {writeResult.MetadataPath}";
    }

    private void LoadResearchEditor(WorkspaceProjectItemViewModel? project)
    {
        if (project is null)
        {
            ResearchEditStatus = "Select a project with command center metadata to edit research.";
            SetResearchEditor(new ApplicationMarketResearchMetadata(), []);
            return;
        }

        var readResult = _commandCenterMetadataService.Read(project.Path);
        if (!readResult.HasMetadata)
        {
            ResearchEditStatus = "Create command center metadata before editing research.";
            SetResearchEditor(new ApplicationMarketResearchMetadata(), []);
            return;
        }

        var projectIntelligence = readResult.Metadata.ProjectIntelligence ?? new ApplicationProjectIntelligenceMetadata();
        SetResearchEditor(
            readResult.Metadata.MarketResearch,
            projectIntelligence.Assumptions);
        ResearchEditStatus = "Research metadata loaded for local editing.";
    }

    private void SetResearchEditor(
        ApplicationMarketResearchMetadata research,
        IReadOnlyList<string> growthAssumptions)
    {
        ResearchTargetAudience = research.TargetAudience ?? string.Empty;
        ResearchPositioning = research.Positioning ?? string.Empty;
        ResearchKeywordsText = JoinList(research.Keywords);
        ResearchCompetitorsText = JoinList(research.Competitors);
        ResearchOpenQuestionsText = JoinList(research.OpenQuestions);
        ResearchGrowthAssumptionsText = JoinList(growthAssumptions);
        ResearchNotes = research.Notes ?? string.Empty;
    }

    private void SaveResearchMetadata()
    {
        if (SelectedProject is null)
        {
            ResearchEditStatus = "Select a project before saving research metadata.";
            return;
        }

        var readResult = _commandCenterMetadataService.Read(SelectedProject.Path);
        if (!readResult.HasMetadata)
        {
            ResearchEditStatus = "Create command center metadata before saving research.";
            return;
        }

        var projectIntelligence = readResult.Metadata.ProjectIntelligence ?? new ApplicationProjectIntelligenceMetadata();
        var metadata = readResult.Metadata with
        {
            MarketResearch = new ApplicationMarketResearchMetadata
            {
                TargetAudience = Optional(ResearchTargetAudience),
                Positioning = Optional(ResearchPositioning),
                Keywords = SplitList(ResearchKeywordsText),
                Competitors = SplitList(ResearchCompetitorsText),
                OpenQuestions = SplitList(ResearchOpenQuestionsText),
                Notes = Optional(ResearchNotes)
            },
            ProjectIntelligence = projectIntelligence with
            {
                Assumptions = SplitList(ResearchGrowthAssumptionsText)
            }
        };

        var writeResult = _commandCenterMetadataService.Write(SelectedProject.Path, metadata);
        if (!writeResult.IsSuccess)
        {
            ResearchEditStatus = $"Research metadata could not be saved: {string.Join(" ", writeResult.Errors)}";
            return;
        }

        SelectedProjectDetail = new WorkspaceProjectDetailViewModel(
            _workspaceProjectDetailService.GetDetail(SelectedProject.Path));
        SetResearchEditor(
            writeResult.Metadata.MarketResearch,
            writeResult.Metadata.ProjectIntelligence.Assumptions);
        ResearchEditStatus = $"Research metadata saved: {writeResult.MetadataPath}";
    }

    private void LoadProjectMetricsEditor(WorkspaceProjectItemViewModel? project)
    {
        if (project is null)
        {
            ProjectMetricsEditStatus = "Select a project with command center metadata to edit project metrics.";
            SetProjectMetricsEditor(new ApplicationProjectIntelligenceMetadata());
            return;
        }

        var readResult = _commandCenterMetadataService.Read(project.Path);
        if (!readResult.HasMetadata)
        {
            ProjectMetricsEditStatus = "Create command center metadata before editing project metrics.";
            SetProjectMetricsEditor(new ApplicationProjectIntelligenceMetadata());
            return;
        }

        SetProjectMetricsEditor(readResult.Metadata.ProjectIntelligence ?? new ApplicationProjectIntelligenceMetadata());
        ProjectMetricsEditStatus = "Project metrics loaded for local editing.";
    }

    private void SetProjectMetricsEditor(ApplicationProjectIntelligenceMetadata intelligence)
    {
        ProjectTargetUsers = intelligence.TargetUsers?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        ProjectTargetDate = intelligence.TargetDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        ProjectReportingCadence = intelligence.ReportingCadence ?? string.Empty;
        ProjectMetricSnapshotsText = JoinMetricSnapshots(intelligence.MetricSnapshots ?? []);
        ProjectMilestoneProgressText = JoinMilestoneProgress(intelligence.MilestoneProgress ?? []);
        ProjectIntelligenceNotes = intelligence.Notes ?? string.Empty;
    }

    private void SaveProjectMetrics()
    {
        if (SelectedProject is null)
        {
            ProjectMetricsEditStatus = "Select a project before saving project metrics.";
            return;
        }

        var readResult = _commandCenterMetadataService.Read(SelectedProject.Path);
        if (!readResult.HasMetadata)
        {
            ProjectMetricsEditStatus = "Create command center metadata before saving project metrics.";
            return;
        }

        if (!TryBuildProjectIntelligence(
                readResult.Metadata.ProjectIntelligence ?? new ApplicationProjectIntelligenceMetadata(),
                out var projectIntelligence,
                out var errors))
        {
            ProjectMetricsEditStatus = $"Project metrics could not be saved: {string.Join(" ", errors)}";
            return;
        }

        var metadata = readResult.Metadata with
        {
            ProjectIntelligence = projectIntelligence
        };

        var writeResult = _commandCenterMetadataService.Write(SelectedProject.Path, metadata);
        if (!writeResult.IsSuccess)
        {
            ProjectMetricsEditStatus = $"Project metrics could not be saved: {string.Join(" ", writeResult.Errors)}";
            return;
        }

        SelectedProjectDetail = new WorkspaceProjectDetailViewModel(
            _workspaceProjectDetailService.GetDetail(SelectedProject.Path));
        SetProjectMetricsEditor(writeResult.Metadata.ProjectIntelligence);
        ProjectMetricsEditStatus = $"Project metrics saved: {writeResult.MetadataPath}";
    }

    private static string? Optional(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string JoinList(IReadOnlyList<string> values)
    {
        return string.Join(System.Environment.NewLine, values);
    }

    private static IReadOnlyList<string> SplitList(string value)
    {
        return value
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private bool TryBuildProjectIntelligence(
        ApplicationProjectIntelligenceMetadata current,
        out ApplicationProjectIntelligenceMetadata projectIntelligence,
        out IReadOnlyList<string> errors)
    {
        var validationErrors = new List<string>();
        var targetUsers = ParseOptionalInt(ProjectTargetUsers, "Target users", validationErrors);
        var targetDate = ParseOptionalDate(ProjectTargetDate, "Target date", validationErrors);
        var metricSnapshots = ParseMetricSnapshots(ProjectMetricSnapshotsText, validationErrors);
        var milestoneProgress = ParseMilestoneProgress(ProjectMilestoneProgressText, validationErrors);

        var latestSnapshot = metricSnapshots
            .OrderBy(snapshot => snapshot.Date)
            .LastOrDefault();
        if (targetUsers is not null && latestSnapshot is not null && targetUsers.Value < latestSnapshot.AcquiredUsers)
        {
            validationErrors.Add("Target users must be greater than or equal to the latest acquired users.");
        }

        if (targetUsers is not null &&
            latestSnapshot is not null &&
            targetUsers.Value > latestSnapshot.AcquiredUsers &&
            targetDate is not null &&
            targetDate.Value <= latestSnapshot.Date)
        {
            validationErrors.Add("Target date must be after the latest metric snapshot date.");
        }

        projectIntelligence = current with
        {
            TargetUsers = targetUsers,
            TargetDate = targetDate,
            ReportingCadence = Optional(ProjectReportingCadence),
            MetricSnapshots = metricSnapshots,
            MilestoneProgress = milestoneProgress,
            Notes = Optional(ProjectIntelligenceNotes)
        };
        errors = validationErrors;
        return validationErrors.Count == 0;
    }

    private static int? ParseOptionalInt(string value, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{label} must be a whole number.");
            return null;
        }

        if (result < 0)
        {
            errors.Add($"{label} cannot be negative.");
            return null;
        }

        return result;
    }

    private static DateOnly? ParseOptionalDate(string value, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        errors.Add($"{label} must be a valid date.");
        return null;
    }

    private static IReadOnlyList<ApplicationProjectMetricSnapshotMetadata> ParseMetricSnapshots(
        string value,
        List<string> errors)
    {
        var snapshots = new List<ApplicationProjectMetricSnapshotMetadata>();
        foreach (var line in SplitList(value))
        {
            var parts = line.Split('|').Select(part => part.Trim()).ToArray();
            if (parts.Length < 3)
            {
                errors.Add("Metric snapshot rows must use date|acquired|active|retention|notes.");
                continue;
            }

            if (!DateOnly.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out var date))
            {
                errors.Add($"Metric snapshot date is invalid: {parts[0]}.");
                continue;
            }

            var acquiredUsers = ParseRequiredInt(parts[1], "Acquired users", errors);
            var activeUsers = ParseRequiredInt(parts[2], "Active users", errors);
            var retentionProxy = ParseOptionalDecimal(parts.Length > 3 ? parts[3] : string.Empty, "Retention proxy", errors);
            if (acquiredUsers is null || activeUsers is null)
            {
                continue;
            }

            if (activeUsers.Value > acquiredUsers.Value)
            {
                errors.Add("Active users cannot exceed acquired users for the same snapshot.");
            }

            snapshots.Add(new ApplicationProjectMetricSnapshotMetadata
            {
                Date = date,
                AcquiredUsers = acquiredUsers.Value,
                ActiveUsers = activeUsers.Value,
                RetentionProxy = retentionProxy,
                Notes = parts.Length > 4 ? Optional(parts[4]) : null
            });
        }

        return snapshots
            .OrderBy(snapshot => snapshot.Date)
            .ToArray();
    }

    private static int? ParseRequiredInt(string value, string label, List<string> errors)
    {
        if (!int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{label} must be a whole number.");
            return null;
        }

        if (result < 0)
        {
            errors.Add($"{label} cannot be negative.");
            return null;
        }

        return result;
    }

    private static decimal? ParseOptionalDecimal(string value, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        errors.Add($"{label} must be a decimal number.");
        return null;
    }

    private static IReadOnlyList<ApplicationProductMilestoneMetadata> ParseMilestoneProgress(
        string value,
        List<string> errors)
    {
        var milestones = new List<ApplicationProductMilestoneMetadata>();
        foreach (var line in SplitList(value))
        {
            var parts = line.Split('|').Select(part => part.Trim()).ToArray();
            if (parts.Length < 3)
            {
                errors.Add("Milestone rows must use id|title|status|progress|notes.");
                continue;
            }

            var progress = ParseOptionalInt(parts.Length > 3 ? parts[3] : string.Empty, "Milestone progress", errors);
            if (progress is < 0 or > 100)
            {
                errors.Add("Milestone progress must be between 0 and 100.");
            }

            milestones.Add(new ApplicationProductMilestoneMetadata
            {
                Id = parts[0],
                Title = parts[1],
                Status = parts[2],
                ProgressPercent = progress,
                Notes = parts.Length > 4 ? Optional(parts[4]) : null
            });
        }

        return milestones.ToArray();
    }

    private static string JoinMetricSnapshots(IReadOnlyList<ApplicationProjectMetricSnapshotMetadata> snapshots)
    {
        return string.Join(
            System.Environment.NewLine,
            snapshots.Select(snapshot =>
                string.Join(
                    "|",
                    snapshot.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    snapshot.AcquiredUsers.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    snapshot.ActiveUsers.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    snapshot.RetentionProxy?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                    snapshot.Notes ?? string.Empty)));
    }

    private static string JoinMilestoneProgress(IReadOnlyList<ApplicationProductMilestoneMetadata> milestones)
    {
        return string.Join(
            System.Environment.NewLine,
            milestones.Select(milestone =>
                string.Join(
                    "|",
                    milestone.Id,
                    milestone.Title,
                    milestone.Status,
                    milestone.ProgressPercent?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                    milestone.Notes ?? string.Empty)));
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
