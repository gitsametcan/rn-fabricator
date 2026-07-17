using Fabricator.Core;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using Fabricator.Core.Workspaces;
using Fabricator.Desktop.WorkspaceSettings;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;

namespace Fabricator.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IWorkspaceDiscoveryService _workspaceDiscoveryService;
    private readonly IWorkspaceProjectDetailService _workspaceProjectDetailService;
    private readonly IWorkspaceSettingsStore _workspaceSettingsStore;
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
    private string _createProjectName = string.Empty;
    private string _createOutputDirectory = string.Empty;
    private string _createTemplateName = CreateProjectService.DefaultStarterId;
    private string _createTemplateSource = string.Empty;
    private string _createFormStatus = "Load a workspace before creating a project.";

    public MainViewModel()
        : this(new WorkspaceDiscoveryService(), new WorkspaceProjectDetailService(), new FileWorkspaceSettingsStore())
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceSettingsStore workspaceSettingsStore)
        : this(workspaceDiscoveryService, new WorkspaceProjectDetailService(), workspaceSettingsStore)
    {
    }

    public MainViewModel(
        IWorkspaceDiscoveryService workspaceDiscoveryService,
        IWorkspaceProjectDetailService workspaceProjectDetailService,
        IWorkspaceSettingsStore workspaceSettingsStore)
    {
        _workspaceDiscoveryService = workspaceDiscoveryService;
        _workspaceProjectDetailService = workspaceProjectDetailService;
        _workspaceSettingsStore = workspaceSettingsStore;
        LoadWorkspaceCommand = new RelayCommand(LoadWorkspaceFromInput);
        SelectProjectCommand = new RelayCommand<WorkspaceProjectItemViewModel>(SelectProject);
        ShowWorkspaceCommand = new RelayCommand(ShowWorkspace);
        ShowTemplatesCommand = new RelayCommand(ShowTemplates);
        ShowCreateCommand = new RelayCommand(ShowCreate);

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
            }
        }
    }

    public bool IsWorkspaceView => !IsTemplatesView && !IsCreateView;

    public string CreateProjectName
    {
        get => _createProjectName;
        set => SetProperty(ref _createProjectName, value);
    }

    public string CreateOutputDirectory
    {
        get => _createOutputDirectory;
        set => SetProperty(ref _createOutputDirectory, value);
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

    public string CreateFormStatus
    {
        get => _createFormStatus;
        private set => SetProperty(ref _createFormStatus, value);
    }

    public bool IsCreateFormEnabled => HasWorkspace;

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

    public IRelayCommand ShowWorkspaceCommand { get; }

    public IRelayCommand ShowTemplatesCommand { get; }

    public IRelayCommand ShowCreateCommand { get; }

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

        var result = _workspaceDiscoveryService.Discover(workspacePath);
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
        IsTemplatesView = false;
        IsCreateView = false;
        WorkspaceStatus = result.WorkspaceExists
            ? $"Workspace loaded from {result.WorkspacePath}"
            : $"Workspace directory was not found: {result.WorkspacePath}";
        ResetCreateFormDefaults(
            result.WorkspacePath,
            result.TemplateCatalog.Path,
            result.WorkspaceExists,
            result.TemplateCatalog.Exists);

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

    private void ShowWorkspace()
    {
        IsTemplatesView = false;
        IsCreateView = false;
    }

    private void ShowTemplates()
    {
        IsCreateView = false;
        IsTemplatesView = true;
    }

    private void ShowCreate()
    {
        IsTemplatesView = false;
        IsCreateView = true;

        if (!HasWorkspace)
        {
            CreateFormStatus = "Load an existing workspace before creating a project.";
        }
    }

    private void ResetCreateFormDefaults(
        string resultWorkspacePath,
        string templateCatalogPath,
        bool workspaceExists,
        bool templateCatalogExists)
    {
        CreateProjectName = string.Empty;
        CreateOutputDirectory = workspaceExists ? resultWorkspacePath : string.Empty;
        CreateTemplateName = CreateProjectService.DefaultStarterId;
        CreateTemplateSource = templateCatalogExists ? templateCatalogPath : string.Empty;
        CreateFormStatus = workspaceExists
            ? templateCatalogExists
                ? "Ready to configure a new project. The workspace template catalog is selected."
                : "Ready to configure a new project. Add a template source or use the embedded starter."
            : "Load an existing workspace before creating a project.";
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
