using Fabricator.Core.Environment;

namespace Fabricator.Tests.Environment;

public sealed class DependencyCheckResultTests
{
    [Fact]
    public void PassedCreatesSuccessfulResultWithoutRemediation()
    {
        var result = DependencyCheckResult.Passed("Node.js", "v20.0.0", "Node.js is installed.");

        Assert.Equal("Node.js", result.Name);
        Assert.Equal(DependencyCheckStatus.Passed, result.Status);
        Assert.Equal("v20.0.0", result.DetectedVersion);
        Assert.Equal("Node.js is installed.", result.Message);
        Assert.Null(result.RemediationHint);
        Assert.False(result.IsRequiredFailure);
    }

    [Fact]
    public void WarningCreatesNonBlockingResultWithRemediation()
    {
        var result = DependencyCheckResult.Warning(
            "Watchman",
            null,
            "Watchman was not found.",
            "Install Watchman for a better React Native development experience.");

        Assert.Equal(DependencyCheckStatus.Warning, result.Status);
        Assert.False(result.IsRequiredFailure);
        Assert.Equal("Install Watchman for a better React Native development experience.", result.RemediationHint);
    }

    [Fact]
    public void FailedCreatesRequiredFailureWithRemediation()
    {
        var result = DependencyCheckResult.Failed(
            "Git",
            null,
            "Git was not found.",
            "Install Git and make sure it is available on PATH.");

        Assert.Equal(DependencyCheckStatus.Failed, result.Status);
        Assert.True(result.IsRequiredFailure);
        Assert.Equal("Install Git and make sure it is available on PATH.", result.RemediationHint);
    }
}
