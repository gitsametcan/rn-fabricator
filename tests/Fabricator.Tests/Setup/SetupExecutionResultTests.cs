using Fabricator.Cli.Setup;

namespace Fabricator.Tests.Setup;

public sealed class SetupExecutionResultTests
{
    [Fact]
    public void CountsStepStatuses()
    {
        var result = new SetupExecutionResult([
            new SetupExecutionStepResult("Node.js", SetupExecutionStepStatus.Succeeded),
            new SetupExecutionStepResult("Watchman", SetupExecutionStepStatus.Failed),
            new SetupExecutionStepResult("Git", SetupExecutionStepStatus.SkippedByUser),
            new SetupExecutionStepResult("CocoaPods", SetupExecutionStepStatus.SkippedByPolicy),
            new SetupExecutionStepResult("Xcode", SetupExecutionStepStatus.ManualOnly),
            new SetupExecutionStepResult("Java", SetupExecutionStepStatus.WouldRun)
        ]);

        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.SkippedByUserCount);
        Assert.Equal(1, result.SkippedByPolicyCount);
        Assert.Equal(1, result.ManualOnlyCount);
        Assert.Equal(1, result.WouldRunCount);
        Assert.True(result.HasFailures);
        Assert.True(result.HasManualOrPolicySkippedWork);
    }

    [Fact]
    public void EmptyResultHasNoFailuresOrManualWork()
    {
        var result = new SetupExecutionResult([]);

        Assert.Equal(0, result.SucceededCount);
        Assert.False(result.HasFailures);
        Assert.False(result.HasManualOrPolicySkippedWork);
    }
}
