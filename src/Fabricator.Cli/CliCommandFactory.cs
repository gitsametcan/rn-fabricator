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
        var command = new Command("templates", "List, inspect, validate, status, add, update, remove, copy, apply, and capture Fabricator templates.");
        var listCommand = new Command("list", "List templates from a Fabricator template catalog.");
        var infoCommand = new Command("info", "Show details for a template from a Fabricator template catalog.");
        var validateCommand = new Command("validate", "Validate a Fabricator template catalog.");
        var statusCommand = new Command("status", "Show templates applied to a compatible Fabricator project.");
        var addCommand = new Command("add", "Capture and register a reusable template in a local Fabricator catalog.");
        var updateCommand = new Command("update", "Refresh an existing local template from a compatible Fabricator project.");
        var removeCommand = new Command("remove", "Remove a template entry from a local Fabricator catalog.");
        var applyCommand = new Command("apply", "Apply a template to a compatible Fabricator project.");
        var captureCommand = new Command("capture", "Capture files from a compatible Fabricator project as a reusable template.");
        var copyCommand = new Command("copy", "Copy a template from a Fabricator template catalog.");
        var templateArgument = new Argument<string>("template")
        {
            Description = "Template id to copy."
        };
        var infoTemplateArgument = new Argument<string>("template")
        {
            Description = "Template id to inspect."
        };
        var applyTemplateArgument = new Argument<string>("template")
        {
            Description = "Template id to apply."
        };
        var addTemplateArgument = new Argument<string>("template")
        {
            Description = "Template id to add."
        };
        var updateTemplateArgument = new Argument<string>("template")
        {
            Description = "Template id to update."
        };
        var removeTemplateArgument = new Argument<string>("template")
        {
            Description = "Template id to remove."
        };
        var captureTemplateArgument = new Argument<string>("template")
        {
            Description = "Template id to capture."
        };
        var sourceOption = new Option<string>("--source")
        {
            Description = "Fabricator template catalog URL or local catalog file path. When omitted, the CLI resolves a local source automatically."
        };
        var categoryOption = new Option<string>("--category")
        {
            Description = "Filter listed templates by category."
        };
        var infoSourceOption = new Option<string>("--source")
        {
            Description = "Fabricator template catalog URL or local catalog file path. When omitted, the CLI resolves a local source automatically."
        };
        var copySourceOption = new Option<string>("--source")
        {
            Description = "Fabricator template catalog URL or local catalog file path. When omitted, the CLI resolves a local source automatically."
        };
        var applySourceOption = new Option<string>("--source")
        {
            Description = "Fabricator template catalog URL or local catalog file path. When omitted, the CLI resolves a local source automatically."
        };
        var validateSourceOption = new Option<string>("--source")
        {
            Description = "Fabricator template catalog URL or local catalog file path. When omitted, the CLI resolves a local source automatically."
        };
        var statusProjectOption = new Option<string>("--project")
        {
            Description = "Compatible Fabricator project directory to inspect.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory()
        };
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Directory where template files will be copied.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory()
        };
        var applyOutputOption = new Option<string>("--output", "-o")
        {
            Description = "Compatible Fabricator project directory where template files will be applied.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory()
        };
        var overwriteOption = new Option<bool>("--overwrite")
        {
            Description = "Overwrite existing files instead of skipping them."
        };
        var applyOverwriteOption = new Option<bool>("--overwrite")
        {
            Description = "Overwrite existing files instead of skipping them."
        };
        var captureCategoryOption = new Option<string>("--category")
        {
            Description = "Fabricator project folder key to capture, such as screens, components, services, or utils."
        };
        var captureFromOption = new Option<string>("--from")
        {
            Description = "Compatible Fabricator project directory to capture from."
        };
        var captureOutputOption = new Option<string>("--output", "-o")
        {
            Description = "Directory where captured template folders will be created.",
            DefaultValueFactory = _ => Path.Combine(Directory.GetCurrentDirectory(), "templates")
        };
        var addCategoryOption = new Option<string>("--category")
        {
            Description = "Fabricator project folder key to capture, such as screens, components, services, or utils."
        };
        var addFromOption = new Option<string>("--from")
        {
            Description = "Compatible Fabricator project directory to capture from."
        };
        var addSourceOption = new Option<string>("--source")
        {
            Description = "Local Fabricator template catalog file path to update."
        };
        var updateFromOption = new Option<string>("--from")
        {
            Description = "Compatible Fabricator project directory to refresh from."
        };
        var updateSourceOption = new Option<string>("--source")
        {
            Description = "Local Fabricator template catalog file path to update."
        };
        var removeSourceOption = new Option<string>("--source")
        {
            Description = "Local Fabricator template catalog file path to update."
        };
        var removeDeleteFilesOption = new Option<bool>("--delete-files")
        {
            Description = "Also delete the template folder when it can be resolved safely."
        };

        listCommand.Options.Add(sourceOption);
        listCommand.Options.Add(categoryOption);
        listCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var source = parseResult.GetValue(sourceOption) ?? string.Empty;
            var category = parseResult.GetValue(categoryOption);
            var handler = TemplatesDependencies.CreateDefaultListHandler(Console.Out, Console.Error);

            return await handler.RunAsync(source, category, cancellationToken);
        });

        infoCommand.Arguments.Add(infoTemplateArgument);
        infoCommand.Options.Add(infoSourceOption);
        infoCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetRequiredValue(infoTemplateArgument);
            var source = parseResult.GetValue(infoSourceOption) ?? string.Empty;
            var handler = TemplatesDependencies.CreateDefaultInfoHandler(Console.Out, Console.Error);

            return await handler.RunAsync(template, source, cancellationToken);
        });

        validateCommand.Options.Add(validateSourceOption);
        validateCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var source = parseResult.GetValue(validateSourceOption) ?? string.Empty;
            var handler = TemplatesDependencies.CreateDefaultValidateHandler(Console.Out, Console.Error);

            return await handler.RunAsync(source, cancellationToken);
        });

        statusCommand.Options.Add(statusProjectOption);
        statusCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var projectDirectory = parseResult.GetValue(statusProjectOption) ?? Directory.GetCurrentDirectory();
            var handler = TemplatesDependencies.CreateDefaultStatusHandler(Console.Out, Console.Error);

            return await handler.RunAsync(projectDirectory, cancellationToken);
        });

        applyCommand.Arguments.Add(applyTemplateArgument);
        applyCommand.Options.Add(applySourceOption);
        applyCommand.Options.Add(applyOutputOption);
        applyCommand.Options.Add(applyOverwriteOption);
        applyCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetRequiredValue(applyTemplateArgument);
            var source = parseResult.GetValue(applySourceOption) ?? string.Empty;
            var outputDirectory = parseResult.GetValue(applyOutputOption) ?? Directory.GetCurrentDirectory();
            var overwrite = parseResult.GetValue(applyOverwriteOption);
            var handler = TemplatesDependencies.CreateDefaultApplyHandler(Console.Out, Console.Error);

            return await handler.RunAsync(template, source, outputDirectory, overwrite, cancellationToken);
        });

        addCommand.Arguments.Add(addTemplateArgument);
        addCommand.Options.Add(addCategoryOption);
        addCommand.Options.Add(addFromOption);
        addCommand.Options.Add(addSourceOption);
        addCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetRequiredValue(addTemplateArgument);
            var category = parseResult.GetValue(addCategoryOption) ?? string.Empty;
            var sourceProjectDirectory = parseResult.GetValue(addFromOption) ?? string.Empty;
            var source = parseResult.GetValue(addSourceOption) ?? string.Empty;
            var handler = TemplatesDependencies.CreateDefaultAddHandler(Console.Out, Console.Error);

            return await handler.RunAsync(template, category, sourceProjectDirectory, source, cancellationToken);
        });

        updateCommand.Arguments.Add(updateTemplateArgument);
        updateCommand.Options.Add(updateFromOption);
        updateCommand.Options.Add(updateSourceOption);
        updateCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetRequiredValue(updateTemplateArgument);
            var sourceProjectDirectory = parseResult.GetValue(updateFromOption) ?? string.Empty;
            var source = parseResult.GetValue(updateSourceOption) ?? string.Empty;
            var handler = TemplatesDependencies.CreateDefaultUpdateHandler(Console.Out, Console.Error);

            return await handler.RunAsync(template, sourceProjectDirectory, source, cancellationToken);
        });

        removeCommand.Arguments.Add(removeTemplateArgument);
        removeCommand.Options.Add(removeSourceOption);
        removeCommand.Options.Add(removeDeleteFilesOption);
        removeCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetRequiredValue(removeTemplateArgument);
            var source = parseResult.GetValue(removeSourceOption) ?? string.Empty;
            var deleteFiles = parseResult.GetValue(removeDeleteFilesOption);
            var handler = TemplatesDependencies.CreateDefaultRemoveHandler(Console.Out, Console.Error);

            return await handler.RunAsync(template, source, deleteFiles, cancellationToken);
        });

        captureCommand.Arguments.Add(captureTemplateArgument);
        captureCommand.Options.Add(captureCategoryOption);
        captureCommand.Options.Add(captureFromOption);
        captureCommand.Options.Add(captureOutputOption);
        captureCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetRequiredValue(captureTemplateArgument);
            var category = parseResult.GetValue(captureCategoryOption) ?? string.Empty;
            var sourceProjectDirectory = parseResult.GetValue(captureFromOption) ?? string.Empty;
            var outputDirectory = parseResult.GetValue(captureOutputOption) ?? Path.Combine(Directory.GetCurrentDirectory(), "templates");
            var handler = TemplatesDependencies.CreateDefaultCaptureHandler(Console.Out, Console.Error);

            return await handler.RunAsync(template, category, sourceProjectDirectory, outputDirectory, cancellationToken);
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
        command.Subcommands.Add(infoCommand);
        command.Subcommands.Add(validateCommand);
        command.Subcommands.Add(statusCommand);
        command.Subcommands.Add(addCommand);
        command.Subcommands.Add(updateCommand);
        command.Subcommands.Add(removeCommand);
        command.Subcommands.Add(applyCommand);
        command.Subcommands.Add(captureCommand);
        command.Subcommands.Add(copyCommand);
        return command;
    }
}
