namespace Fabricator.Core.Toolchains;

public enum ToolchainRecommendationStrategy
{
    ExactVersion,
    MinimumVersion,
    SupportedRange,
    LatestStable,
    Lts,
    Manual
}
