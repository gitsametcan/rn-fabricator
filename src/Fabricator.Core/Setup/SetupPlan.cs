namespace Fabricator.Core.Setup;

public sealed record SetupPlan(
    string PlatformName,
    PackageManagerInfo PackageManager,
    IReadOnlyList<SetupPlanItem> Items)
{
    public bool HasItems => Items.Count > 0;

    public int CommandCount => Items.Count(item => item.Kind == SetupPlanItemKind.Command);

    public int ManualCount => Items.Count(item => item.Kind == SetupPlanItemKind.Manual);

    public int EnvironmentCount => Items.Count(item => item.Kind == SetupPlanItemKind.Environment);
}
