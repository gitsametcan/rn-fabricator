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
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(ExitCodes.Success, "created", string.Empty));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("MyApp", "basic-auth", outputDirectory.Path));

        Assert.True(result.Succeeded);
        Assert.Equal(Path.Combine(outputDirectory.Path, "MyApp"), result.ProjectPath);
        Assert.NotNull(result.Command);
        Assert.Equal("npx", result.Command.FileName);
        Assert.Equal(["@react-native-community/cli@latest", "init", "MyApp"], result.Command.Arguments);
        Assert.Equal(outputDirectory.Path, result.Command.WorkingDirectory);
        Assert.Collection(runner.Requests, request => Assert.Same(result.Command, request));
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
