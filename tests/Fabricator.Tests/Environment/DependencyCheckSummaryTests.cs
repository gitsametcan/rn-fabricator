using Fabricator.Core;
using Fabricator.Core.Environment;

namespace Fabricator.Tests.Environment;

public sealed class DependencyCheckSummaryTests
{
    [Fact]
    public void SummaryCountsResultsByStatus()
    {
        var summary = new DependencyCheckSummary(
        [
            DependencyCheckResult.Passed("Node.js", "v20.0.0", "Node.js is installed."),
            DependencyCheckResult.Warning("Watchman", null, "Watchman was not found.", "Install Watchman."),
            DependencyCheckResult.Failed("Git", null, "Git was not found.", "Install Git.")
        ]);

        Assert.Equal(1, summary.PassedCount);
        Assert.Equal(1, summary.WarningCount);
        Assert.Equal(1, summary.FailedCount);
    }

    [Fact]
    public void SummaryReturnsSuccessExitCodeWhenNoRequiredFailuresExist()
    {
        var summary = new DependencyCheckSummary(
        [
            DependencyCheckResult.Passed("Node.js", "v20.0.0", "Node.js is installed."),
            DependencyCheckResult.Warning("Watchman", null, "Watchman was not found.", "Install Watchman.")
        ]);

        Assert.False(summary.HasRequiredFailures);
        Assert.Equal(ExitCodes.Success, summary.ExitCode);
    }

    [Fact]
    public void SummaryReturnsEnvironmentFailureExitCodeWhenRequiredFailuresExist()
    {
        var summary = new DependencyCheckSummary(
        [
            DependencyCheckResult.Failed("Git", null, "Git was not found.", "Install Git.")
        ]);

        Assert.True(summary.HasRequiredFailures);
        Assert.Equal(ExitCodes.EnvironmentFailure, summary.ExitCode);
    }
}
