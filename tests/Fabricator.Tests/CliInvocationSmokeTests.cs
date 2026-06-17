using Fabricator.Cli;
using Fabricator.Cli.Doctor;
using Fabricator.Cli.Projects;
using Fabricator.Core;
using Fabricator.Core.Environment;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;
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
