using Fabricator.Core;
using Fabricator.Core.Environment;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.Environment;

public sealed class DependencyCheckServiceTests
{
    [Fact]
    public async Task CheckCoreToolsAsyncRunsNodeNpmAndGitVersionCommands()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "v20.11.1\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "10.2.4\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "git version 2.39.5\n", string.Empty));
        var service = new DependencyCheckService(runner);

        var summary = await service.CheckCoreToolsAsync();

        Assert.Equal(3, summary.PassedCount);
        Assert.Equal(ExitCodes.Success, summary.ExitCode);
        Assert.Collection(
            runner.Requests,
            request =>
            {
                Assert.Equal("node", request.FileName);
                Assert.Equal(["--version"], request.Arguments);
            },
            request =>
            {
                Assert.Equal("npm", request.FileName);
                Assert.Equal(["--version"], request.Arguments);
            },
            request =>
            {
                Assert.Equal("git", request.FileName);
                Assert.Equal(["--version"], request.Arguments);
            });
    }

    [Fact]
    public async Task CheckCoreToolsAsyncExtractsDetectedVersions()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "v20.11.1\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "10.2.4\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "git version 2.39.5\n", string.Empty));
        var service = new DependencyCheckService(runner);

        var summary = await service.CheckCoreToolsAsync();

        Assert.Collection(
            summary.Results,
            result =>
            {
                Assert.Equal("Node.js", result.Name);
                Assert.Equal("v20.11.1", result.DetectedVersion);
            },
            result =>
            {
                Assert.Equal("npm", result.Name);
                Assert.Equal("10.2.4", result.DetectedVersion);
            },
            result =>
            {
                Assert.Equal("Git", result.Name);
                Assert.Equal("2.39.5", result.DetectedVersion);
            });
    }

    [Fact]
    public async Task CheckCoreToolsAsyncMapsFailedProcessToDependencyFailure()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "v20.11.1\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "npm failed"));
        runner.Enqueue(new ProcessRunResult(0, "git version 2.39.5\n", string.Empty));
        var service = new DependencyCheckService(runner);

        var summary = await service.CheckCoreToolsAsync();

        Assert.True(summary.HasRequiredFailures);
        Assert.Equal(ExitCodes.EnvironmentFailure, summary.ExitCode);
        Assert.Equal(2, summary.PassedCount);
        Assert.Equal(1, summary.FailedCount);

        var npmResult = summary.Results.Single(result => result.Name == "npm");
        Assert.Equal(DependencyCheckStatus.Failed, npmResult.Status);
        Assert.Null(npmResult.DetectedVersion);
        Assert.Contains("npm was not found or returned exit code 1.", npmResult.Message);
        Assert.Equal("Install npm and make sure `npm` is available on PATH.", npmResult.RemediationHint);
    }

    [Fact]
    public async Task CheckCoreToolsAsyncAllowsSuccessfulCommandWithoutVersionOutput()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "10.2.4\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "git version 2.39.5\n", string.Empty));
        var service = new DependencyCheckService(runner);

        var summary = await service.CheckCoreToolsAsync();

        var nodeResult = summary.Results.Single(result => result.Name == "Node.js");
        Assert.Equal(DependencyCheckStatus.Passed, nodeResult.Status);
        Assert.Null(nodeResult.DetectedVersion);
        Assert.Equal("Node.js is installed.", nodeResult.Message);
    }
}
