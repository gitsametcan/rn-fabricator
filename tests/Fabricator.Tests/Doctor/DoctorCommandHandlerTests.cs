using Fabricator.Cli.Doctor;
using Fabricator.Core;
using Fabricator.Core.Environment;

namespace Fabricator.Tests.Doctor;

public sealed class DoctorCommandHandlerTests
{
    [Fact]
    public async Task RunAsyncChecksEveryDependencyGroupAndReturnsSuccessWhenNoFailuresExist()
    {
        var service = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Passed("Node.js", "v20.11.1", "Node.js is installed.")
            ]),
            AppleToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Warning("Watchman", null, "Watchman was not found.", "Install Watchman.")
            ]),
            AndroidToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Passed("Java", "17", "Java is installed.")
            ])
        };
        using var writer = new StringWriter();
        var handler = new DoctorCommandHandler(service, new DoctorSummaryRenderer(writer));

        var exitCode = await handler.RunAsync();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal(1, service.CoreToolsCallCount);
        Assert.Equal(1, service.AppleToolsCallCount);
        Assert.Equal(1, service.AndroidToolsCallCount);
        Assert.Contains("Passed: 2, Warnings: 1, Failed: 0", writer.ToString());
    }

    [Fact]
    public async Task RunAsyncReturnsEnvironmentFailureWhenAnyDependencyFails()
    {
        var service = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Git", null, "Git was not found.", "Install Git.")
            ])
        };
        using var writer = new StringWriter();
        var handler = new DoctorCommandHandler(service, new DoctorSummaryRenderer(writer));

        var exitCode = await handler.RunAsync();

        Assert.Equal(ExitCodes.EnvironmentFailure, exitCode);
        Assert.Contains("[FAIL] Git", writer.ToString());
        Assert.Contains("Hint: Install Git.", writer.ToString());
    }
}
