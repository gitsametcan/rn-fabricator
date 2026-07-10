using Fabricator.Core;

namespace Fabricator.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    public string ProductTagline { get; } = "Desktop companion for React Native CLI project setup.";

    public string PageTitle { get; } = "Project workspace";

    public string PageSubtitle { get; } =
        "A stable shell for future diagnostics, setup planning, project creation, and template workflows.";

    public string MilestoneLabel { get; } = $"v{ProductInfo.Version} UI foundation";

    public string CurrentFocus { get; } =
        "Establish the Avalonia desktop application skeleton before wiring product workflows.";

    public string CommandBoundary { get; } =
        "Desktop screens should reuse Fabricator.Core services and shared application adapters instead of depending on console rendering.";

    public IReadOnlyList<ShellNavigationItem> NavigationItems { get; } =
    [
        new("Dashboard", "Workspace overview and next actions."),
        new("Doctor", "Environment diagnostics for React Native tooling."),
        new("Setup", "Guided dependency planning and safe apply flow."),
        new("Templates", "Catalog inspection and reusable template lifecycle.")
    ];

    public IReadOnlyList<ShellNavigationItem> WorkflowCards { get; } =
    [
        new("Environment diagnostics", "Surface doctor results with readable status and remediation hints."),
        new("Guided setup", "Preview setup plans and keep machine mutations explicit."),
        new("Project creation", "Collect create inputs and show generated project next steps."),
        new("Template catalog", "Inspect, validate, apply, and track reusable templates.")
    ];
}
