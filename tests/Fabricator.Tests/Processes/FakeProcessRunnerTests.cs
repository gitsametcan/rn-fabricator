using Fabricator.Core;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.Processes;

public sealed class FakeProcessRunnerTests
{
    [Fact]
    public async Task RunAsyncReturnsConfiguredResultAndRecordsRequest()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "v1.0.0", string.Empty));

        var result = await runner.RunAsync(ProcessRunRequest.Create("node", "--version"));

        Assert.True(result.Succeeded);
        Assert.Equal("v1.0.0", result.StandardOutput);
        Assert.Collection(
            runner.Requests,
            request =>
            {
                Assert.Equal("node", request.FileName);
                Assert.Equal(["--version"], request.Arguments);
            });
    }

    [Fact]
    public async Task RunAsyncReturnsFailureWhenNoResultIsConfigured()
    {
        var runner = new FakeProcessRunner();

        var result = await runner.RunAsync(ProcessRunRequest.Create("missing-tool"));

        Assert.False(result.Succeeded);
        Assert.Equal(ExitCodes.GeneralFailure, result.ExitCode);
        Assert.Contains("No fake result configured.", result.StandardError);
    }
}
