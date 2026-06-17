using Fabricator.Core;
using Fabricator.Cli.Doctor;
using Fabricator.Core.Projects;
using System.CommandLine;

namespace Fabricator.Cli;

public static class CliCommandFactory
{
    public static RootCommand CreateRootCommand()
    {
        return CreateRootCommand(() => DoctorDependencies.CreateDefaultHandler(Console.Out));
    }

    public static RootCommand CreateRootCommand(Func<DoctorCommandHandler> doctorHandlerFactory)
    {
        var rootCommand = new RootCommand(ProductInfo.Description);

        rootCommand.Subcommands.Add(CreateDoctorCommand(doctorHandlerFactory));
        rootCommand.Subcommands.Add(CreateCreateCommand());

        return rootCommand;
    }

    private static Command CreateDoctorCommand(Func<DoctorCommandHandler> doctorHandlerFactory)
    {
        var command = new Command("doctor", "Check React Native CLI development environment requirements.");

        command.SetAction(async (_, cancellationToken) =>
        {
            var handler = doctorHandlerFactory();
            return await handler.RunAsync(cancellationToken);
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

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Directory where the React Native project will be created.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory()
        };

        var command = new Command("create", "Create a new React Native CLI project.");
        command.Arguments.Add(nameArgument);
        command.Options.Add(templateOption);
        command.Options.Add(outputOption);

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetRequiredValue(nameArgument);
            var template = parseResult.GetValue(templateOption) ?? "basic-auth";
            var outputDirectory = parseResult.GetValue(outputOption) ?? Directory.GetCurrentDirectory();
            var validation = new CreateProjectValidator().Validate(
                new CreateProjectRequest(name, template, outputDirectory));

            if (!validation.IsValid)
            {
                Console.Error.WriteLine("Invalid create command input:");

                foreach (var error in validation.Errors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return ExitCodes.InvalidInput;
            }

            Console.WriteLine(
                $"create is not implemented yet. Requested project: {validation.Request.ProjectName}, template: {validation.Request.TemplateName}, output: {validation.FullProjectPath}");
            return ExitCodes.Success;
        });

        return command;
    }
}
