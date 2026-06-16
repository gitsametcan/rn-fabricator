using Fabricator.Core;
using System.CommandLine;

namespace Fabricator.Cli;

public static class CliCommandFactory
{
    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand(ProductInfo.Description);

        rootCommand.Subcommands.Add(CreateDoctorCommand());
        rootCommand.Subcommands.Add(CreateCreateCommand());

        return rootCommand;
    }

    private static Command CreateDoctorCommand()
    {
        var command = new Command("doctor", "Check React Native CLI development environment requirements.");

        command.SetAction(_ =>
        {
            Console.WriteLine("doctor checks are not implemented yet.");
            return ExitCodes.Success;
        });

        return command;
    }

    private static Command CreateCreateCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "React Native project name to create."
        };

        var templateOption = new Option<string>("--template", "-t")
        {
            Description = "Starter template to apply.",
            DefaultValueFactory = _ => "basic-auth"
        };

        var command = new Command("create", "Create a new React Native CLI project.");
        command.Arguments.Add(nameArgument);
        command.Options.Add(templateOption);

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetRequiredValue(nameArgument);
            var template = parseResult.GetValue(templateOption) ?? "basic-auth";

            Console.WriteLine($"create is not implemented yet. Requested project: {name}, template: {template}");
            return ExitCodes.Success;
        });

        return command;
    }
}
