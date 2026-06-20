using Fabricator.Core;
using Fabricator.Core.Projects;
using Fabricator.Cli.Doctor;
using Fabricator.Cli.Projects;
using Fabricator.Cli.Setup;
using Fabricator.Cli.Templates;
using System.CommandLine;

namespace Fabricator.Cli;

public static class CliCommandFactory
{
    public static RootCommand CreateRootCommand()
    {
        return CreateRootCommand(
            () => DoctorDependencies.CreateDefaultHandler(Console.Out),
            () => CreateProjectDependencies.CreateDefaultHandler(Console.Out, Console.Error),
            () => SetupDependencies.CreateDefaultPlanHandler(Console.Out),
            () => SetupDependencies.CreateDefaultApplyHandler(Console.In, Console.Out));
    }

    public static RootCommand CreateRootCommand(Func<DoctorCommandHandler> doctorHandlerFactory)
    {
        return CreateRootCommand(
            doctorHandlerFactory,
            () => CreateProjectDependencies.CreateDefaultHandler(Console.Out, Console.Error),
            () => SetupDependencies.CreateDefaultPlanHandler(Console.Out),
            () => SetupDependencies.CreateDefaultApplyHandler(Console.In, Console.Out));
    }

    public static RootCommand CreateRootCommand(
        Func<DoctorCommandHandler> doctorHandlerFactory,
        Func<CreateCommandHandler> createHandlerFactory)
    {
        return CreateRootCommand(
            doctorHandlerFactory,
            createHandlerFactory,
            () => SetupDependencies.CreateDefaultPlanHandler(Console.Out),
            () => SetupDependencies.CreateDefaultApplyHandler(Console.In, Console.Out));
    }

    public static RootCommand CreateRootCommand(
        Func<DoctorCommandHandler> doctorHandlerFactory,
        Func<CreateCommandHandler> createHandlerFactory,
        Func<SetupPlanCommandHandler> setupPlanHandlerFactory)
    {
        return CreateRootCommand(
            doctorHandlerFactory,
            createHandlerFactory,
            setupPlanHandlerFactory,
            () => SetupDependencies.CreateDefaultApplyHandler(Console.In, Console.Out));
    }

    public static RootCommand CreateRootCommand(
        Func<DoctorCommandHandler> doctorHandlerFactory,
        Func<CreateCommandHandler> createHandlerFactory,
        Func<SetupPlanCommandHandler> setupPlanHandlerFactory,
        Func<SetupApplyCommandHandler> setupApplyHandlerFactory)
    {
        var rootCommand = new RootCommand(ProductInfo.Description);
        ConfigureVersionOption(rootCommand);

        rootCommand.Subcommands.Add(CreateDoctorCommand(doctorHandlerFactory));
        rootCommand.Subcommands.Add(CreateSetupCommand(setupPlanHandlerFactory, setupApplyHandlerFactory));
        rootCommand.Subcommands.Add(CreateCreateCommand(createHandlerFactory));
        rootCommand.Subcommands.Add(CreateTemplatesCommand());

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

    private static Command CreateSetupCommand(
        Func<SetupPlanCommandHandler> setupPlanHandlerFactory,
        Func<SetupApplyCommandHandler> setupApplyHandlerFactory)
    {
        var command = new Command(
            "setup",
            "Plan guided React Native environment setup. The doctor command is read-only; setup prints next actions.");
        var planCommand = new Command(
            "plan",
            "Run read-only environment checks and print setup actions without executing install commands.");
        var applyCommand = new Command(
            "apply",
            "Run setup plan and apply safe command steps after per-step confirmation.");
        var profileOption = new Option<string>("--profile", "-p")
        {
            Description = "Toolchain profile id to use for setup recommendations."
        };
        var reactNativeOption = new Option<string>("--react-native")
        {
            Description = "React Native version to use for setup recommendations."
        };
        var applyProfileOption = new Option<string>("--profile", "-p")
        {
            Description = "Toolchain profile id to use for setup recommendations."
        };
        var applyReactNativeOption = new Option<string>("--react-native")
        {
            Description = "React Native version to use for setup recommendations."
        };
        var applyDryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show eligible setup commands without executing them."
        };
        var applyYesOption = new Option<bool>("--yes")
        {
            Description = "Run safe allowlisted setup commands without per-step prompts."
        };

        planCommand.Options.Add(profileOption);
        planCommand.Options.Add(reactNativeOption);
        applyCommand.Options.Add(applyProfileOption);
        applyCommand.Options.Add(applyReactNativeOption);
        applyCommand.Options.Add(applyDryRunOption);
        applyCommand.Options.Add(applyYesOption);

        planCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(profileOption);
            var reactNativeVersion = parseResult.GetValue(reactNativeOption);
            var handler = setupPlanHandlerFactory();
            return await handler.RunAsync(profile, reactNativeVersion, cancellationToken);
        });

        applyCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(applyProfileOption);
            var reactNativeVersion = parseResult.GetValue(applyReactNativeOption);
            var dryRun = parseResult.GetValue(applyDryRunOption);
            var yes = parseResult.GetValue(applyYesOption);
            var handler = setupApplyHandlerFactory();
            return await handler.RunAsync(profile, reactNativeVersion, dryRun, yes, cancellationToken);
        });

        command.Subcommands.Add(planCommand);
        command.Subcommands.Add(applyCommand);
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
            DefaultValueFactory = _ => CreateProjectService.DefaultStarterId
        };

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Directory where the React Native project will be created.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory()
        };
        var templateSourceOption = new Option<string>("--template-source")
        {
            Description = "Fabricator template catalog URL or local catalog file path."
        };

        var command = new Command("create", "Create a new React Native CLI project.");
        command.Arguments.Add(nameArgument);
        command.Options.Add(templateOption);
        command.Options.Add(outputOption);
        command.Options.Add(templateSourceOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetRequiredValue(nameArgument);
            var template = parseResult.GetValue(templateOption) ?? CreateProjectService.DefaultStarterId;
            var outputDirectory = parseResult.GetValue(outputOption) ?? Directory.GetCurrentDirectory();
            var templateSource = parseResult.GetValue(templateSourceOption);

            var handler = createHandlerFactory();
            return await handler.RunAsync(name, template, outputDirectory, templateSource, cancellationToken);
        });

        return command;
    }

    private static Command CreateTemplatesCommand()
    {
        var command = new Command("templates", "List and copy Fabricator templates from a catalog source.");
        var listCommand = new Command("list", "List templates from a Fabricator template catalog.");
        var copyCommand = new Command("copy", "Copy a template from a Fabricator template catalog.");
        var templateArgument = new Argument<string>("template")
        {
            Description = "Template id to copy."
        };
        var sourceOption = new Option<string>("--source")
        {
            Description = "Fabricator template catalog URL or local catalog file path."
        };
        var copySourceOption = new Option<string>("--source")
        {
            Description = "Fabricator template catalog URL or local catalog file path."
        };
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Directory where template files will be copied.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory()
        };
        var overwriteOption = new Option<bool>("--overwrite")
        {
            Description = "Overwrite existing files instead of skipping them."
        };

        listCommand.Options.Add(sourceOption);
        listCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var source = parseResult.GetValue(sourceOption) ?? string.Empty;
            var handler = TemplatesDependencies.CreateDefaultListHandler(Console.Out, Console.Error);

            return await handler.RunAsync(source, cancellationToken);
        });

        copyCommand.Arguments.Add(templateArgument);
        copyCommand.Options.Add(copySourceOption);
        copyCommand.Options.Add(outputOption);
        copyCommand.Options.Add(overwriteOption);
        copyCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetRequiredValue(templateArgument);
            var source = parseResult.GetValue(copySourceOption) ?? string.Empty;
            var outputDirectory = parseResult.GetValue(outputOption) ?? Directory.GetCurrentDirectory();
            var overwrite = parseResult.GetValue(overwriteOption);
            var handler = TemplatesDependencies.CreateDefaultCopyHandler(Console.Out, Console.Error);

            return await handler.RunAsync(template, source, outputDirectory, overwrite, cancellationToken);
        });

        command.Subcommands.Add(listCommand);
        command.Subcommands.Add(copyCommand);
        return command;
    }
}
