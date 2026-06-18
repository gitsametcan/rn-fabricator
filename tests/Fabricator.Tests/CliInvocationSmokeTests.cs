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

namespace Fabricator.Tests;

[Collection("ConsoleOutput")]
public sealed class CliInvocationSmokeTests
{
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
        Assert.Contains("Template selected: basic-auth", output.ToString());
        Assert.Contains("Example config files: not generated yet", output.ToString());
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
            rollback ?? CreateProjectRollbackResult.NotRequired("Rollback was not required."));
    }
}
