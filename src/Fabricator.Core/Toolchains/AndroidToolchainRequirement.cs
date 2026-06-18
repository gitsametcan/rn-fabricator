namespace Fabricator.Core.Toolchains;

public sealed record AndroidToolchainRequirement(
    int CompileSdk,
    int? TargetSdk,
    int? MinSdk,
    ToolchainRequirement PlatformTools,
    IReadOnlyList<string>? RequiredPackages = null)
{
    public IReadOnlyList<string> RequiredPackages { get; init; } = RequiredPackages ?? [];
}
