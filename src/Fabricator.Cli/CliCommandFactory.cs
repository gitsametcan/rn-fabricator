using Fabricator.Core;
using Fabricator.Cli.Doctor;
using Fabricator.Cli.Projects;
using System.CommandLine;

namespace Fabricator.Cli;

public static class CliCommandFactory
{
    public static RootCommand CreateRootCommand()
    {
        return CreateRootCommand(
            () => DoctorDependencies.CreateDefaultHandler(Console.Out),
            () => CreateProjectDependencies.CreateDefaultHandler(Console.Out, Console.Error));
    }

    public static RootCommand CreateRootCommand(Func<DoctorCommandHandler> doctorHandlerFactory)
    {
        return CreateRootCommand(
            doctorHandlerFactory,
            () => CreateProjectDependencies.CreateDefaultHandler(Console.Out, Console.Error));
    }

    public static RootCommand CreateRootCommand(
        Func<DoctorCommandHandler> doctorHandlerFactory,
        Func<CreateCommandHandler> createHandlerFactory)
    {
        var rootCommand = new RootCommand(ProductInfo.Description);
        ConfigureVersionOption(rootCommand);

        rootCommand.Subcommands.Add(CreateDoctorCommand(doctorHandlerFactory));
        rootCommand.Subcommands.Add(CreateCreateCommand(createHandlerFactory));

        return rootCommand;
    }

    private static void ConfigureVersionOption(RootCommand rootCommand)
    {
        var defaultVersionOption = rootCommand.Options.OfType<VersionOption>().SingleOrDefault();
        if (defaultVersionOption is not null)
        {
            rootCommand.Options.Remove(defaultVersionOption);
        }

        rootCommand.Options.Add(new VersionOption
        {
            Action = new ProductVersionAction()
        });
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

    private static Command CreateCreateCommand(Func<CreateCommandHandler> createHandlerFactory)
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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetRequiredValue(nameArgument);
            var template = parseResult.GetValue(templateOption) ?? "basic-auth";
            var outputDirectory = parseResult.GetValue(outputOption) ?? Directory.GetCurrentDirectory();

            var handler = createHandlerFactory();
            return await handler.RunAsync(name, template, outputDirectory, cancellationToken);
        });

        return command;
    }
}
