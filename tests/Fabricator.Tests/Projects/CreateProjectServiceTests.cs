using Fabricator.Core;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;
using System.Text.Json;

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
        Assert.Contains("src/screens/MainScreen.tsx", result.StarterResult.GeneratedFiles);
        Assert.Contains("src/utils/index.ts", result.StarterResult.GeneratedFiles);
        Assert.Contains(".fabricator/project.json", result.StarterResult.GeneratedFiles);
        Assert.Contains("fabricator.json", result.StarterResult.GeneratedFiles);
        var appContent = File.ReadAllText(Path.Combine(projectPath, "App.tsx"));
        Assert.Contains("SplashScreen", appContent);
        Assert.Contains("MainScreen", appContent);
        Assert.True(File.Exists(Path.Combine(projectPath, "src", "screens", "SplashScreen.tsx")));
        Assert.True(File.Exists(Path.Combine(projectPath, "src", "screens", "MainScreen.tsx")));
        Assert.True(File.Exists(Path.Combine(projectPath, "src", "utils", "index.ts")));
        AssertProjectManifest(projectPath);
        AssertProjectState(
            projectPath,
            expectedProjectName: "MyApp",
            expectedTemplateId: CreateProjectService.DefaultStarterId,
            expectedTemplateSourceType: "embedded",
            expectedTemplateSourceValue: CreateProjectService.DefaultStarterId);
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
    public async Task CreateAsyncAppliesStarterFromTemplateCatalogSource()
    {
        using var outputDirectory = new TemporaryDirectory();
        var projectPath = Path.Combine(outputDirectory.Path, "MyApp");
        var catalogPath = WriteMinimalCatalog(outputDirectory.Path, mode: "starter");
        var runner = new FakeProcessRunner
        {
            OnRun = _ => Directory.CreateDirectory(projectPath)
        };
        runner.Enqueue(new ProcessRunResult(ExitCodes.Success, "created", string.Empty));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest(
                "MyApp",
                CreateProjectService.DefaultStarterId,
                outputDirectory.Path,
                TemplateSource: catalogPath));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.StarterResult);
        Assert.Equal(CreateProjectService.DefaultStarterId, result.StarterResult.StarterId);
        Assert.Contains("App.tsx", result.StarterResult.GeneratedFiles);
        Assert.Contains(".fabricator/project.json", result.StarterResult.GeneratedFiles);
        Assert.Contains("fabricator.json", result.StarterResult.GeneratedFiles);
        Assert.Equal("catalog app\n", File.ReadAllText(Path.Combine(projectPath, "App.tsx")));
        AssertProjectManifest(projectPath);
        AssertProjectState(
            projectPath,
            expectedProjectName: "MyApp",
            expectedTemplateId: CreateProjectService.DefaultStarterId,
            expectedTemplateSourceType: "local",
            expectedTemplateSourceValue: catalogPath);
    }

    [Fact]
    public async Task CreateAsyncDoesNotOverwriteExistingFabricatorStateFile()
    {
        using var outputDirectory = new TemporaryDirectory();
        var projectPath = Path.Combine(outputDirectory.Path, "MyApp");
        var existingState = """
            {
              "schemaVersion": 1,
              "kind": "custom-state"
            }
            """;
        var runner = new FakeProcessRunner
        {
            OnRun = _ =>
            {
                Directory.CreateDirectory(projectPath);
                File.WriteAllText(Path.Combine(projectPath, "fabricator.json"), existingState);
            }
        };
        runner.Enqueue(new ProcessRunResult(ExitCodes.Success, "created", string.Empty));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("MyApp", CreateProjectService.DefaultStarterId, outputDirectory.Path));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.StarterResult);
        Assert.DoesNotContain("fabricator.json", result.StarterResult.GeneratedFiles);
        Assert.Equal(existingState, File.ReadAllText(Path.Combine(projectPath, "fabricator.json")));
    }

    [Fact]
    public async Task CreateAsyncFailsWhenCatalogTemplateIsNotAStarter()
    {
        using var outputDirectory = new TemporaryDirectory();
        var projectPath = Path.Combine(outputDirectory.Path, "MyApp");
        var catalogPath = WriteMinimalCatalog(outputDirectory.Path, mode: "copy");
        var runner = new FakeProcessRunner
        {
            OnRun = _ => Directory.CreateDirectory(projectPath)
        };
        runner.Enqueue(new ProcessRunResult(ExitCodes.Success, "created", string.Empty));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest(
                "MyApp",
                CreateProjectService.DefaultStarterId,
                outputDirectory.Path,
                TemplateSource: catalogPath));

        Assert.False(result.Succeeded);
        Assert.Equal(ExitCodes.GeneralFailure, result.ExitCode);
        Assert.False(Directory.Exists(projectPath));
        Assert.Contains("not a create starter", result.ProcessResult?.StandardError);
        Assert.True(result.Rollback.Attempted);
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

    private static string WriteMinimalCatalog(string root, string mode)
    {
        var templateRoot = Path.Combine(root, "minimal-splash");
        Directory.CreateDirectory(Path.Combine(templateRoot, "src", "screens"));

        File.WriteAllText(
            Path.Combine(root, "catalog.fabricator.json"),
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-template-catalog",
              "displayName": "Test catalog",
              "description": "Test catalog.",
              "templates": [
                {
                  "id": "minimal-splash",
                  "displayName": "Minimal Splash",
                  "description": "Minimal starter.",
                  "version": "0.1.0",
                  "manifest": "minimal-splash/fabricator-template.json",
                  "tags": ["starter"]
                }
              ]
            }
            """);

        File.WriteAllText(
            Path.Combine(templateRoot, "fabricator-template.json"),
            $$"""
            {
              "schemaVersion": 1,
              "kind": "fabricator-template",
              "id": "minimal-splash",
              "displayName": "Minimal Splash",
              "description": "Minimal starter.",
              "version": "0.1.0",
              "mode": "{{mode}}",
              "files": [
                {
                  "path": "App.tsx",
                  "type": "file"
                },
                {
                  "path": "src/screens/index.ts",
                  "type": "file"
                }
              ]
            }
            """);

        File.WriteAllText(Path.Combine(templateRoot, "App.tsx"), "catalog app\n");
        File.WriteAllText(Path.Combine(templateRoot, "src", "screens", "index.ts"), "export {};\n");

        return Path.Combine(root, "catalog.fabricator.json");
    }

    private static void AssertProjectManifest(string projectPath)
    {
        var manifestPath = Path.Combine(projectPath, ".fabricator", "project.json");

        Assert.True(File.Exists(manifestPath));

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("fabricator-react-native-project", root.GetProperty("kind").GetString());
        Assert.Equal("react-native-cli", root.GetProperty("projectType").GetString());
        Assert.Equal("src", root.GetProperty("sourceRoot").GetString());
        Assert.Contains(
            root.GetProperty("folders").EnumerateArray(),
            folder => folder.GetProperty("key").GetString() == "screens" &&
                      folder.GetProperty("path").GetString() == "src/screens");
        Assert.Contains(
            root.GetProperty("integrationPoints").EnumerateArray(),
            point => point.GetProperty("key").GetString() == "screensBarrel" &&
                     point.GetProperty("type").GetString() == "barrel-export");
    }

    private static void AssertProjectState(
        string projectPath,
        string expectedProjectName,
        string expectedTemplateId,
        string expectedTemplateSourceType,
        string expectedTemplateSourceValue)
    {
        var statePath = Path.Combine(projectPath, "fabricator.json");

        Assert.True(File.Exists(statePath));

        using var document = JsonDocument.Parse(File.ReadAllText(statePath));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("fabricator-project-state", root.GetProperty("kind").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("toolVersion").GetString()));
        Assert.Equal(expectedProjectName, root.GetProperty("project").GetProperty("name").GetString());
        Assert.Equal("react-native-cli", root.GetProperty("project").GetProperty("type").GetString());
        Assert.True(root.GetProperty("project").GetProperty("createdAt").TryGetDateTimeOffset(out _));

        var source = Assert.Single(root.GetProperty("templateSources").EnumerateArray());
        Assert.Equal(expectedTemplateSourceType, source.GetProperty("type").GetString());
        Assert.Equal(expectedTemplateSourceValue, source.GetProperty("value").GetString());
        Assert.True(source.GetProperty("isDefault").GetBoolean());

        var appliedTemplate = Assert.Single(root.GetProperty("appliedTemplates").EnumerateArray());
        Assert.Equal(expectedTemplateId, appliedTemplate.GetProperty("id").GetString());
        Assert.Equal("0.1.0", appliedTemplate.GetProperty("version").GetString());
        Assert.Equal("starter", appliedTemplate.GetProperty("category").GetString());
        Assert.Equal("create", appliedTemplate.GetProperty("operation").GetString());
        Assert.Equal("applied", appliedTemplate.GetProperty("result").GetString());
        Assert.True(appliedTemplate.GetProperty("appliedAt").TryGetDateTimeOffset(out _));
        Assert.Equal(expectedTemplateSourceType, appliedTemplate.GetProperty("source").GetProperty("type").GetString());
        Assert.Equal(expectedTemplateSourceValue, appliedTemplate.GetProperty("source").GetProperty("value").GetString());
        Assert.Contains(
            appliedTemplate.GetProperty("files").GetProperty("written").EnumerateArray(),
            file => file.GetString() == "App.tsx");
        Assert.Empty(appliedTemplate.GetProperty("files").GetProperty("skipped").EnumerateArray());
        Assert.Empty(appliedTemplate.GetProperty("files").GetProperty("overwritten").EnumerateArray());
        Assert.Empty(appliedTemplate.GetProperty("exports").EnumerateArray());
        Assert.Empty(appliedTemplate.GetProperty("integrationNotes").EnumerateArray());
    }
}
