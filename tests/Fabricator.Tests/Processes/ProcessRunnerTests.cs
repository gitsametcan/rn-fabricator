using Fabricator.Core;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.Processes;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsyncCapturesStandardOutput()
    {
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(ProcessRunRequest.Create("dotnet", "--version"));

        Assert.True(result.Succeeded, result.StandardError);
        Assert.NotEmpty(result.StandardOutput.Trim());
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public async Task RunAsyncReturnsFailureResultWhenExecutableIsMissing()
    {
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(ProcessRunRequest.Create("rn-fabricator-missing-executable"));

        Assert.False(result.Succeeded);
        Assert.Equal(ExitCodes.GeneralFailure, result.ExitCode);
        Assert.NotEmpty(result.StandardError);
    }
}
