using Fabricator.Cli;
using Fabricator.Cli.Doctor;
using Fabricator.Cli.Projects;
using Fabricator.Cli.Setup;
using Fabricator.Core;
using Fabricator.Core.Environment;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;
using Fabricator.Core.Setup;
using System.CommandLine;
using System.Reflection;
using System.Text.Json;

namespace Fabricator.Tests;

[Collection("ConsoleOutput")]
public sealed class CliInvocationSmokeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Fact]
    public void VersionOptionWritesCleanPackageVersion()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var expectedVersion = typeof(CliCommandFactory)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["--version"]).Invoke();

        var versionOutput = output.ToString().Trim();
        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal(expectedVersion, versionOutput);
        Assert.DoesNotContain("+", versionOutput);
    }

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
        var rootCommand = CreateRootCommandWithCreateResult(
            BuildCreateResult("MyApp", "basic-auth", Directory.GetCurrentDirectory()));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(
            ["create", "MyApp", "--template", "basic-auth", "--output", Directory.GetCurrentDirectory()]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Creating React Native project: MyApp", output.ToString());
        Assert.Contains("Command: npx @react-native-community/cli@latest init MyApp", output.ToString());
        Assert.Contains("React Native project created:", output.ToString());
        Assert.Contains(Path.Combine(Directory.GetCurrentDirectory(), "MyApp"), output.ToString());
        Assert.Contains("Next steps:", output.ToString());
        Assert.Contains("1. cd MyApp", output.ToString());
        Assert.Contains("2. npm start", output.ToString());
        Assert.Contains("3. npm run ios", output.ToString());
        Assert.Contains("4. npm run android", output.ToString());
        Assert.Contains("Starter applied: minimal-splash", output.ToString());
        Assert.Contains("Starter files generated:", output.ToString());
        Assert.Contains("Template selected: basic-auth", output.ToString());
    }

    [Fact]
    public void TemplatesListCommandReturnsSuccessAndWritesCatalogTemplates()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["templates", "list", "--source", source]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Fabricator templates", output.ToString());
        Assert.Contains($"Source: {source}", output.ToString());
        Assert.Contains("minimal-splash (0.1.0)", output.ToString());
        Assert.Contains("Basic Auth", output.ToString());
        Assert.Contains("Category: starter", output.ToString());
        Assert.Contains("Category: auth", output.ToString());
    }

    [Fact]
    public void TemplatesListCommandFiltersTemplatesByCategory()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["templates", "list", "--source", source, "--category", "auth"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Category: auth", output.ToString());
        Assert.Contains("basic-auth (0.1.0)", output.ToString());
        Assert.DoesNotContain("minimal-splash (0.1.0)", output.ToString());
    }

    [Fact]
    public void TemplatesListCommandReturnsClearOutputWhenCategoryHasNoMatches()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["templates", "list", "--source", source, "--category", "screen"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("No templates found for category: screen", output.ToString());
    }

    [Fact]
    public void TemplatesListCommandReturnsInvalidInputForEmptyCategory()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["templates", "list", "--source", source, "--category", string.Empty]).Invoke();

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Contains("Template category cannot be empty", output.ErrorOutput);
    }

    [Fact]
    public void TemplatesInfoCommandReturnsSuccessAndWritesTemplateDetails()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["templates", "info", "basic-auth", "--source", source]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Template: basic-auth (0.1.0)", output.ToString());
        Assert.Contains("Category: auth", output.ToString());
        Assert.Contains("Tags: auth, screen, config", output.ToString());
        Assert.Contains("Files: 11", output.ToString());
        Assert.Contains("Target path: src/auth/AuthProvider.tsx", output.ToString());
        Assert.Contains("Exports: 4", output.ToString());
        Assert.Contains("Integration hints: 2", output.ToString());
    }

    [Fact]
    public void TemplatesInfoCommandReturnsInvalidInputForUnknownTemplate()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["templates", "info", "missing-template", "--source", source]).Invoke();

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Contains("Template could not be read.", output.ErrorOutput);
        Assert.Contains("missing-template", output.ErrorOutput);
    }

    [Fact]
    public void TemplatesCopyCommandCopiesTemplateFilesToOutputDirectory()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var outputDirectory = CreateTemporaryDirectory();

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(
                ["templates", "copy", "basic-auth", "--source", source, "--output", outputDirectory]).Invoke();

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Contains("Template copied: basic-auth", output.ToString());
            Assert.Contains("Generated:", output.ToString());
            Assert.True(File.Exists(Path.Combine(outputDirectory, "App.tsx")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, ".env.example")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "src", "auth", "AuthProvider.tsx")));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectory);
        }
    }

    [Fact]
    public void TemplatesCopyCommandSkipsExistingFilesByDefault()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var outputDirectory = CreateTemporaryDirectory();
        var appPath = Path.Combine(outputDirectory, "App.tsx");
        File.WriteAllText(appPath, "existing app\n");

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(
                ["templates", "copy", "basic-auth", "--source", source, "--output", outputDirectory]).Invoke();

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Contains("Skipped:", output.ToString());
            Assert.Contains("- App.tsx", output.ToString());
            Assert.Equal("existing app\n", File.ReadAllText(appPath));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectory);
        }
    }

    [Fact]
    public void TemplatesApplyCommandAppliesTemplateFilesToCompatibleProject()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var outputDirectory = CreateCompatibleFabricatorProject();

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(
                ["templates", "apply", "basic-auth", "--source", source, "--output", outputDirectory]).Invoke();

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Contains("Template applied: basic-auth", output.ToString());
            Assert.Contains("Generated:", output.ToString());
            Assert.Contains("Exports applied: 4", output.ToString());
            Assert.Contains("Integration notes: 2", output.ToString());
            Assert.True(File.Exists(Path.Combine(outputDirectory, "App.tsx")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, ".env.example")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "src", "auth", "AuthProvider.tsx")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "src", "screens", "HomeScreen.tsx")));
            Assert.Contains(
                "export { HomeScreen } from './HomeScreen';",
                File.ReadAllText(Path.Combine(outputDirectory, "src", "screens", "index.ts")));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectory);
        }
    }

    [Fact]
    public void TemplatesApplyCommandSkipsExistingFilesByDefault()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var outputDirectory = CreateCompatibleFabricatorProject();
        var appPath = Path.Combine(outputDirectory, "App.tsx");
        File.WriteAllText(appPath, "existing app\n");

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(
                ["templates", "apply", "basic-auth", "--source", source, "--output", outputDirectory]).Invoke();

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Contains("Skipped:", output.ToString());
            Assert.Contains("- App.tsx", output.ToString());
            Assert.Contains("Exports applied: 4", output.ToString());
            Assert.Equal("existing app\n", File.ReadAllText(appPath));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectory);
        }
    }

    [Fact]
    public void TemplatesApplyCommandReturnsFailureForNonFabricatorProject()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var outputDirectory = CreateTemporaryDirectory();

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(
                ["templates", "apply", "basic-auth", "--source", source, "--output", outputDirectory]).Invoke();

            Assert.Equal(ExitCodes.GeneralFailure, exitCode);
            Assert.Contains("Template apply failed.", output.ErrorOutput);
            Assert.Contains("Fabricator project manifest was not found", output.ErrorOutput);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectory);
        }
    }

    [Fact]
    public void TemplatesCaptureCommandCapturesProjectFolderAsTemplate()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var projectDirectory = CreateCompatibleFabricatorProject();
        var outputDirectory = CreateTemporaryDirectory();
        File.WriteAllText(
            Path.Combine(projectDirectory, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(
                [
                    "templates",
                    "capture",
                    "profile-screen",
                    "--category",
                    "screens",
                    "--from",
                    projectDirectory,
                    "--output",
                    outputDirectory
                ]).Invoke();

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Contains("Template captured: profile-screen", output.ToString());
            Assert.Contains("Captured files:", output.ToString());
            Assert.True(File.Exists(Path.Combine(outputDirectory, "profile-screen", "fabricator-template.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "profile-screen", "src", "screens", "ProfileScreen.tsx")));
        }
        finally
        {
            DeleteTemporaryDirectory(projectDirectory);
            DeleteTemporaryDirectory(outputDirectory);
        }
    }

    [Fact]
    public void TemplatesCopyCommandReturnsInvalidInputForUnknownTemplate()
    {
        var rootCommand = CliCommandFactory.CreateRootCommand();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var outputDirectory = CreateTemporaryDirectory();

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(
                ["templates", "copy", "missing-template", "--source", source, "--output", outputDirectory]).Invoke();

            Assert.Equal(ExitCodes.InvalidInput, exitCode);
            Assert.Contains("Template could not be read.", output.ErrorOutput);
            Assert.Contains("missing-template", output.ErrorOutput);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectory);
        }
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file: {relativePath}");
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rn-fabricator-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateCompatibleFabricatorProject()
    {
        var path = CreateTemporaryDirectory();
        var manifest = FabricatorProjectContract.CreateManifest(ProductInfo.Version);

        Directory.CreateDirectory(Path.Combine(path, ".fabricator"));
        Directory.CreateDirectory(Path.Combine(path, "src"));

        foreach (var folder in manifest.Folders)
        {
            Directory.CreateDirectory(Path.Combine(path, folder.Path));
        }

        foreach (var integrationPoint in manifest.IntegrationPoints)
        {
            var integrationPath = Path.Combine(path, integrationPoint.Path);
            var directory = Path.GetDirectoryName(integrationPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(integrationPath, "export {};\n");
        }

        File.WriteAllText(
            Path.Combine(path, FabricatorProjectContract.ManifestRelativePath),
            JsonSerializer.Serialize(manifest, JsonOptions));

        return path;
    }

    private static void DeleteTemporaryDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Fact]
    public void SetupPlanCommandReturnsSuccessAndWritesPlanOutput()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = new SetupPlan(
                "macOS",
                new PackageManagerInfo("Homebrew", true, "brew"),
                [
                    new SetupPlanItem(
                        "Watchman",
                        SetupPlanItemKind.Command,
                        "Install Watchman",
                        ["brew install watchman"])
                ])
        };
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(new FakeCreateProjectService(), Console.Out, Console.Error),
            () => new SetupPlanCommandHandler(setupPlanService, new SetupPlanRenderer(Console.Out)));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["setup", "plan"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal(1, setupPlanService.CallCount);
        Assert.Contains("React Native setup plan", output.ToString());
        Assert.Contains("[command] Watchman: Install Watchman", output.ToString());
        Assert.Contains("No install commands were executed.", output.ToString());
    }

    [Fact]
    public void SetupPlanCommandPassesReactNativeVersionToHandler()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = new SetupPlan(
                "macOS",
                PackageManagerInfo.NotDetected(),
                [])
        };
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(new FakeCreateProjectService(), Console.Out, Console.Error),
            () => new SetupPlanCommandHandler(setupPlanService, new SetupPlanRenderer(Console.Out)));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["setup", "plan", "--react-native", "0.76.x"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal("0.76.x", setupPlanService.LastRequest?.ReactNativeVersion);
        Assert.Null(setupPlanService.LastRequest?.ProfileId);
        Assert.Contains("React Native setup plan", output.ToString());
    }

    [Fact]
    public void SetupPlanCommandReturnsInvalidInputWhenProfileAndReactNativeAreProvided()
    {
        var setupPlanService = new FakeSetupPlanService();
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(new FakeCreateProjectService(), Console.Out, Console.Error),
            () => new SetupPlanCommandHandler(setupPlanService, new SetupPlanRenderer(Console.Out), Console.Error));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse([
            "setup",
            "plan",
            "--profile",
            "react-native-stable",
            "--react-native",
            "0.76.x"
        ]).Invoke();

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Equal(0, setupPlanService.CallCount);
        Assert.Contains("Choose either --profile or --react-native, not both.", output.ErrorOutput);
    }

    [Fact]
    public void SetupPlanCommandReturnsInvalidInputWhenProfileLookupFails()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            ExceptionToThrow = new SetupPlanException([
                "Unsupported React Native toolchain profile: 0.99.x."
            ])
        };
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(new FakeCreateProjectService(), Console.Out, Console.Error),
            () => new SetupPlanCommandHandler(setupPlanService, new SetupPlanRenderer(Console.Out), Console.Error));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["setup", "plan", "--react-native", "0.99.x"]).Invoke();

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Contains("Invalid setup plan input:", output.ErrorOutput);
        Assert.Contains("Unsupported React Native toolchain profile: 0.99.x.", output.ErrorOutput);
    }

    [Fact]
    public void SetupApplyCommandReturnsSuccessAndExecutesAcceptedCommand()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = new SetupPlan(
                "macOS",
                new PackageManagerInfo("Homebrew", true, "brew"),
                [
                    new SetupPlanItem(
                        "Watchman",
                        SetupPlanItemKind.Command,
                        "Install Watchman",
                        ["brew install watchman"])
                ])
        };
        var processRunner = new FakeProcessRunner();
        processRunner.Enqueue(new ProcessRunResult(ExitCodes.Success, "installed", string.Empty));
        using var reader = new StringReader("y\n");
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(new FakeCreateProjectService(), Console.Out, Console.Error),
            () => new SetupPlanCommandHandler(setupPlanService, new SetupPlanRenderer(Console.Out)),
            () => new SetupApplyCommandHandler(
                setupPlanService,
                new SetupPlanRenderer(Console.Out),
                new SetupExecutionResultRenderer(Console.Out),
                processRunner,
                reader,
                Console.Out,
                Console.Error));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["setup", "apply"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Single(processRunner.Requests);
        Assert.Contains("Setup apply", output.ToString());
        Assert.Contains("[succeeded] Watchman", output.ToString());
    }

    [Fact]
    public void SetupApplyCommandPassesReactNativeVersionToHandler()
    {
        var setupPlanService = new FakeSetupPlanService();
        var processRunner = new FakeProcessRunner();
        using var reader = new StringReader(string.Empty);
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(new FakeCreateProjectService(), Console.Out, Console.Error),
            () => new SetupPlanCommandHandler(setupPlanService, new SetupPlanRenderer(Console.Out)),
            () => new SetupApplyCommandHandler(
                setupPlanService,
                new SetupPlanRenderer(Console.Out),
                new SetupExecutionResultRenderer(Console.Out),
                processRunner,
                reader,
                Console.Out,
                Console.Error));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["setup", "apply", "--react-native", "0.76.x"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal("0.76.x", setupPlanService.LastRequest?.ReactNativeVersion);
        Assert.Null(setupPlanService.LastRequest?.ProfileId);
    }

    [Fact]
    public void SetupApplyCommandSupportsDryRunMode()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = new SetupPlan(
                "macOS",
                new PackageManagerInfo("Homebrew", true, "brew"),
                [
                    new SetupPlanItem(
                        "Watchman",
                        SetupPlanItemKind.Command,
                        "Install Watchman",
                        ["brew install watchman"])
                ])
        };
        var processRunner = new FakeProcessRunner();
        using var reader = new StringReader(string.Empty);
        var rootCommand = CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(new FakeCreateProjectService(), Console.Out, Console.Error),
            () => new SetupPlanCommandHandler(setupPlanService, new SetupPlanRenderer(Console.Out)),
            () => new SetupApplyCommandHandler(
                setupPlanService,
                new SetupPlanRenderer(Console.Out),
                new SetupExecutionResultRenderer(Console.Out),
                processRunner,
                reader,
                Console.Out,
                Console.Error));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["setup", "apply", "--dry-run"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Empty(processRunner.Requests);
        Assert.Contains("Mode: dry-run", output.ToString());
        Assert.Contains("[dry-run] Watchman: would run brew install watchman", output.ToString());
    }

    [Fact]
    public void CreateCommandReturnsSuccessAndUsesDefaultTemplate()
    {
        var rootCommand = CreateRootCommandWithCreateResult(
            BuildCreateResult("MyApp", "basic-auth", Directory.GetCurrentDirectory()));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["create", "MyApp"]).Invoke();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Creating React Native project: MyApp", output.ToString());
        Assert.Contains("Next steps:", output.ToString());
        Assert.Contains("1. cd MyApp", output.ToString());
    }

    [Fact]
    public void CreateCommandReturnsInvalidInputForInvalidProjectName()
    {
        var validation = new CreateProjectValidator().Validate(
            new CreateProjectRequest("my-app", "basic-auth", Directory.GetCurrentDirectory()));
        var rootCommand = CreateRootCommandWithCreateResult(CreateProjectResult.Invalid(validation));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["create", "my-app"]).Invoke();

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Contains("Invalid create command input:", output.ErrorOutput);
        Assert.Contains("Project name must start with a letter", output.ErrorOutput);
    }

    [Fact]
    public void CreateCommandReturnsInvalidInputWhenTargetProjectPathExists()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"rn-fabricator-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "MyApp"));
        var validation = new CreateProjectValidator().Validate(
            new CreateProjectRequest("MyApp", "basic-auth", outputDirectory));
        var rootCommand = CreateRootCommandWithCreateResult(CreateProjectResult.Invalid(validation));

        try
        {
            using var output = ConsoleOutputScope.Capture();
            var exitCode = rootCommand.Parse(["create", "MyApp", "--output", outputDirectory]).Invoke();

            Assert.Equal(ExitCodes.InvalidInput, exitCode);
            Assert.Contains("Target project path already exists", output.ErrorOutput);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateCommandReturnsProcessExitCodeAndWritesFailureOutput()
    {
        var rootCommand = CreateRootCommandWithCreateResult(
            BuildCreateResult(
                "MyApp",
                "basic-auth",
                Directory.GetCurrentDirectory(),
                new ProcessRunResult(1, "partial output", "React Native CLI failed."),
                CreateProjectRollbackResult.Completed("Removed partial project directory: /tmp/MyApp")));

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["create", "MyApp"]).Invoke();

        Assert.Equal(1, exitCode);
        Assert.Contains("React Native project creation failed.", output.ErrorOutput);
        Assert.Contains("partial output", output.ErrorOutput);
        Assert.Contains("React Native CLI failed.", output.ErrorOutput);
        Assert.Contains("Rollback completed: Removed partial project directory: /tmp/MyApp", output.ErrorOutput);
    }

    private static RootCommand CreateRootCommandWithCreateResult(CreateProjectResult result)
    {
        var createProjectService = new FakeCreateProjectService();
        createProjectService.Enqueue(result);

        return CliCommandFactory.CreateRootCommand(
            () => new DoctorCommandHandler(new FakeDependencyCheckService(), new DoctorSummaryRenderer(Console.Out)),
            () => new CreateCommandHandler(createProjectService, Console.Out, Console.Error));
    }

    private static CreateProjectResult BuildCreateResult(
        string projectName,
        string templateName,
        string outputDirectory,
        ProcessRunResult? processResult = null,
        CreateProjectRollbackResult? rollback = null)
    {
        var validation = new CreateProjectValidator().Validate(
            new CreateProjectRequest(projectName, templateName, outputDirectory));
        var command = new ProcessRunRequest(
            "npx",
            ["@react-native-community/cli@latest", "init", projectName],
            validation.FullOutputDirectory);

        return CreateProjectResult.Completed(
            validation,
            command,
            processResult ?? new ProcessRunResult(ExitCodes.Success, "created", string.Empty),
            rollback ?? CreateProjectRollbackResult.NotRequired("Rollback was not required."),
            processResult is null || processResult.Succeeded
                ? CreateProjectStarterResult.Applied(CreateProjectService.DefaultStarterId, ["App.tsx"])
                : null);
    }
}
