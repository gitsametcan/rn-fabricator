namespace Fabricator.Core.Toolchains;

public sealed record ToolchainRequirement(
    string Name,
    ToolchainRecommendationStrategy Strategy,
    string? RecommendedVersion = null,
    string? MinimumVersion = null,
    string? SupportedVersionRange = null,
    bool Required = true,
    IReadOnlyList<string>? Notes = null)
{
    public IReadOnlyList<string> Notes { get; init; } = Notes ?? [];
}
