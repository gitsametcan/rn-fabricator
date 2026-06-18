using Fabricator.Cli;
using Fabricator.Core;

namespace Fabricator.Tests;

public sealed class CliCommandFactoryTests
{
    [Fact]
    public void RootCommandUsesProductDescription()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();

        Assert.Equal(ProductInfo.Description, rootCommand.Description);
    }

    [Fact]
    public void RootCommandRegistersMvpCommands()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var commandNames = rootCommand.Subcommands.Select(command => command.Name).ToArray();

        Assert.Contains("doctor", commandNames);
        Assert.Contains("setup", commandNames);
        Assert.Contains("create", commandNames);
    }

    [Fact]
    public void SetupCommandRegistersPlanSubcommand()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var setupCommand = rootCommand.Subcommands.Single(command => command.Name == "setup");
        var subcommandNames = setupCommand.Subcommands.Select(command => command.Name).ToArray();

        Assert.Contains("plan", subcommandNames);
        Assert.Contains("doctor command is read-only", setupCommand.Description);
    }

    [Fact]
    public void SetupPlanCommandDocumentsReadOnlyBehavior()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var setupCommand = rootCommand.Subcommands.Single(command => command.Name == "setup");
        var planCommand = setupCommand.Subcommands.Single(command => command.Name == "plan");

        Assert.Contains("read-only", planCommand.Description);
        Assert.Contains("without executing install commands", planCommand.Description);
    }

    [Fact]
    public void SetupPlanCommandDefinesProfileOptions()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var setupCommand = rootCommand.Subcommands.Single(command => command.Name == "setup");
        var planCommand = setupCommand.Subcommands.Single(command => command.Name == "plan");

        Assert.Contains(
            planCommand.Options,
            option => option.Name == "--profile" && option.Aliases.Contains("-p"));
        Assert.Contains(
            planCommand.Options,
            option => option.Name == "--react-native");
        Assert.Empty(rootCommand.Parse(["setup", "plan", "--react-native", "0.76.x"]).Errors);
    }

    [Fact]
    public void CreateCommandAcceptsProjectNameAndTemplateOption()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var parseResult = rootCommand.Parse(["create", "MyApp", "--template", "basic-auth"]);

        Assert.Empty(parseResult.Errors);
        Assert.Equal("create", parseResult.CommandResult.Command.Name);
    }

    [Fact]
    public void CreateCommandDefaultsToBasicAuthTemplate()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var parseResult = rootCommand.Parse(["create", "MyApp"]);

        Assert.Empty(parseResult.Errors);
        Assert.Equal("create", parseResult.CommandResult.Command.Name);
    }

    [Fact]
    public void CreateCommandDefinesExpectedArgumentAndTemplateOption()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var createCommand = rootCommand.Subcommands.Single(command => command.Name == "create");

        Assert.Contains(createCommand.Arguments, argument => argument.Name == "name");
        Assert.Contains(
            createCommand.Options,
            option => option.Name == "--template" && option.Aliases.Contains("-t"));
        Assert.Contains(
            createCommand.Options,
            option => option.Name == "--output" && option.Aliases.Contains("-o"));
    }

    [Fact]
    public void RootCommandAcceptsHelpAndVersionOptions()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();

        Assert.Empty(rootCommand.Parse(["--help"]).Errors);
        Assert.Empty(rootCommand.Parse(["--version"]).Errors);
    }
}
