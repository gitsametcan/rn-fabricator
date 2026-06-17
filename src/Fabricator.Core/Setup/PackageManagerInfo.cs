namespace Fabricator.Core.Setup;

public sealed record PackageManagerInfo(
    string Name,
    bool IsAvailable,
    string? CommandName = null)
{
    public static PackageManagerInfo NotDetected()
    {
        return new PackageManagerInfo("manual", false);
    }
}
