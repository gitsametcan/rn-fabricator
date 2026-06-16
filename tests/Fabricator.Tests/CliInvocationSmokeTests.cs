using Fabricator.Cli;
using Fabricator.Core;
using System.CommandLine;

namespace Fabricator.Tests;

[Collection("ConsoleOutput")]
public sealed class CliInvocationSmokeTests
{
    [Fact]
    public void DoctorCommandReturnsSuccessAndWritesPlaceholderOutput()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["doctor"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("doctor checks are not implemented yet.", output.ToString());
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
