using Fabricator.Core;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;

namespace Fabricator.Tests.Projects;

public sealed class CreateProjectServiceTests
{
    [Fact]
    public async Task CreateAsyncReturnsInvalidInputWithoutRunningProcessWhenRequestIsInvalid()
    {
        using var outputDirectory = new TemporaryDirectory();
        var runner = new FakeProcessRunner();
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("my-app", "basic-auth", outputDirectory.Path));

        Assert.False(result.Succeeded);
        Assert.Equal(ExitCodes.InvalidInput, result.ExitCode);
        Assert.Null(result.Command);
        Assert.Null(result.ProcessResult);
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task CreateAsyncRunsReactNativeCliInOutputDirectory()
    {
        using var outputDirectory = new TemporaryDirectory();
        var projectPath = Path.Combine(outputDirectory.Path, "MyApp");
        var preparedCommands = new List<ProcessRunRequest>();
        var runner = new FakeProcessRunner
        {
            OnRun = _ => Directory.CreateDirectory(projectPath)
        };
        runner.Enqueue(new ProcessRunResult(ExitCodes.Success, "created", string.Empty));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest(
                "MyApp",
                "basic-auth",
                outputDirectory.Path,
                preparedCommands.Add));

        Assert.True(result.Succeeded);
        Assert.Equal(projectPath, result.ProjectPath);
        Assert.NotNull(result.Command);
        Assert.Equal("npx", result.Command.FileName);
        Assert.Equal(["@react-native-community/cli@latest", "init", "MyApp"], result.Command.Arguments);
        Assert.Equal(outputDirectory.Path, result.Command.WorkingDirectory);
        Assert.NotNull(result.StarterResult);
        Assert.Equal(CreateProjectService.DefaultStarterId, result.StarterResult.StarterId);
        Assert.Contains("App.tsx", result.StarterResult.GeneratedFiles);
        Assert.Contains("src/screens/SplashScreen.tsx", result.StarterResult.GeneratedFiles);
        Assert.Contains("src/utils/index.ts", result.StarterResult.GeneratedFiles);
        Assert.Contains("SplashScreen", File.ReadAllText(Path.Combine(projectPath, "App.tsx")));
        Assert.True(File.Exists(Path.Combine(projectPath, "src", "screens", "SplashScreen.tsx")));
        Assert.True(File.Exists(Path.Combine(projectPath, "src", "utils", "index.ts")));
        Assert.Collection(runner.Requests, request => Assert.Same(result.Command, request));
        Assert.Collection(preparedCommands, command => Assert.Same(result.Command, command));
    }

    [Fact]
    public async Task CreateAsyncFailsWhenReactNativeCliDoesNotCreateProjectDirectory()
    {
        using var outputDirectory = new TemporaryDirectory();
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(ExitCodes.Success, "created", string.Empty));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("MyApp", CreateProjectService.DefaultStarterId, outputDirectory.Path));

        Assert.False(result.Succeeded);
        Assert.Equal(ExitCodes.GeneralFailure, result.ExitCode);
        Assert.NotNull(result.StarterResult);
        Assert.False(result.StarterResult.Succeeded);
        Assert.Contains("Generated project directory was not found", result.ProcessResult?.StandardError);
    }

    [Fact]
    public async Task CreateAsyncPreservesReactNativeCliFailureDetails()
    {
        using var outputDirectory = new TemporaryDirectory();
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(1, "partial output", "cli failed"));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("MyApp", "basic-auth", outputDirectory.Path));

        Assert.False(result.Succeeded);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("partial output", result.ProcessResult?.StandardOutput);
        Assert.Equal("cli failed", result.ProcessResult?.StandardError);
        Assert.False(result.Rollback.Attempted);
        Assert.Contains("no partial project directory", result.Rollback.Message);
    }

    [Fact]
    public async Task CreateAsyncRemovesPartialProjectDirectoryWhenReactNativeCliFails()
    {
        using var outputDirectory = new TemporaryDirectory();
        var projectPath = Path.Combine(outputDirectory.Path, "MyApp");
        var runner = new FakeProcessRunner
        {
            OnRun = _ => Directory.CreateDirectory(projectPath)
        };
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "cli failed"));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("MyApp", "basic-auth", outputDirectory.Path));

        Assert.False(result.Succeeded);
        Assert.True(result.Rollback.Attempted);
        Assert.True(result.Rollback.Succeeded);
        Assert.False(Directory.Exists(projectPath));
        Assert.Contains("Removed partial project directory", result.Rollback.Message);
    }

    [Fact]
    public async Task CreateAsyncDoesNotDeleteTargetFileDuringRollback()
    {
        using var outputDirectory = new TemporaryDirectory();
        var projectPath = Path.Combine(outputDirectory.Path, "MyApp");
        var runner = new FakeProcessRunner
        {
            OnRun = _ => File.WriteAllText(projectPath, "not a directory")
        };
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "cli failed"));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("MyApp", "basic-auth", outputDirectory.Path));

        Assert.False(result.Succeeded);
        Assert.True(result.Rollback.Attempted);
        Assert.False(result.Rollback.Succeeded);
        Assert.True(File.Exists(projectPath));
        Assert.Contains("target path is a file", result.Rollback.Message);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rn-fabricator-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
