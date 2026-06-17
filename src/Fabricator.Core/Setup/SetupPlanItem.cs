namespace Fabricator.Core.Setup;

public sealed record SetupPlanItem(
    string DependencyName,
    SetupPlanItemKind Kind,
    string Title,
    IReadOnlyList<string> Steps,
    bool RequiresAdmin = false);
