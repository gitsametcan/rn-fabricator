namespace Fabricator.Core.Setup;

public sealed record SetupPlanRequest(
    string? ProfileId = null,
    string? ReactNativeVersion = null)
{
    public static SetupPlanRequest Default { get; } = new();
}
