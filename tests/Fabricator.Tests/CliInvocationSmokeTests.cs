using Fabricator.Cli;
using Fabricator.Cli.Doctor;
using Fabricator.Core;
using Fabricator.Core.Environment;
using System.CommandLine;

namespace Fabricator.Tests;

[Collection("ConsoleOutput")]
public sealed class CliInvocationSmokeTests
{
    [Fact]
    public void DoctorCommandReturnsSuccessAndWritesSummaryOutput()
    {
        var service = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Passed("Node.js", "v20.11.1", "Node.js is installed.")
            ])
        };
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(service, new DoctorSummaryRenderer(Console.Out)));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["doctor"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("React Native environment checks", output.ToString());
        Assert.Contains("[PASS] Node.js", output.ToString());
    }

    [Fact]
    public void DoctorCommandReturnsEnvironmentFailureWhenSummaryHasFailure()
    {
        var service = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Git", null, "Git was not found.", "Install Git.")
            ])
        };
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(service, new DoctorSummaryRenderer(Console.Out)));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["doctor"]).Invoke();

        Assert.Equal(ExitCodes.EnvironmentFailure, exitCode);
        Assert.Contains("[FAIL] Git", output.ToString());
    }

    [Fact]
    public void CreateCommandReturnsSuccessAndUsesProvidedTemplate()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["create", "MyApp", "--template", "basic-auth"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Requested project: MyApp, template: basic-auth", output.ToString());
    }

    [Fact]
    public void CreateCommandReturnsSuccessAndUsesDefaultTemplate()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["create", "MyApp"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Requested project: MyApp, template: basic-auth", output.ToString());
    }
}
