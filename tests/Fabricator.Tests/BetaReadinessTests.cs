using Fabricator.Cli;
using Fabricator.Core;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using System.Text.Json;

namespace Fabricator.Tests;

[Collection("ConsoleOutput")]
public sealed class BetaReadinessTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Fact]
    public async Task CreateServiceGeneratesFabricatorProjectStateForBetaStarter()
    {
        using var output = new TemporaryDirectory();
        var projectPath = Path.Combine(output.Path, "BetaApp");
        var runner = new FakeProcessRunner
        {
            OnRun = _ => Directory.CreateDirectory(projectPath)
        };
        runner.Enqueue(new ProcessRunResult(ExitCodes.Success, "created", string.Empty));
        var service = new CreateProjectService(new CreateProjectValidator(), runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest("BetaApp", CreateProjectService.DefaultStarterId, output.Path));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.StarterResult);
        Assert.Contains(".fabricator/project.json", result.StarterResult.GeneratedFiles);
        Assert.Contains("fabricator.json", result.StarterResult.GeneratedFiles);
        Assert.True(File.Exists(Path.Combine(projectPath, ".fabricator", "project.json")));
        Assert.True(File.Exists(Path.Combine(projectPath, "fabricator.json")));

        using var state = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectPath, "fabricator.json")));
        var root = state.RootElement;
        Assert.Equal("fabricator-project-state", root.GetProperty("kind").GetString());
        Assert.Equal("BetaApp", root.GetProperty("project").GetProperty("name").GetString());
        Assert.Equal("embedded", root.GetProperty("templateSources")[0].GetProperty("type").GetString());

        var appliedTemplate = Assert.Single(root.GetProperty("appliedTemplates").EnumerateArray());
        Assert.Equal(CreateProjectService.DefaultStarterId, appliedTemplate.GetProperty("id").GetString());
        Assert.Equal("create", appliedTemplate.GetProperty("operation").GetString());
        Assert.Equal("applied", appliedTemplate.GetProperty("result").GetString());
    }

    [Fact]
    public void TemplateSourceResolverUsesLocalCatalogFromFabricatorState()
    {
        using var project = CreateCompatibleProject();
        var catalogPath = WriteLocalCatalog(project.Path);
        WriteTemplateSourceState(project.Path, "./templates/catalog.fabricator.json");
        var resolver = new TemplateSourceResolver(new EmptyEnvironmentVariables());

        var result = resolver.Resolve(new TemplateSourceResolutionRequest(
            ExplicitSource: string.Empty,
            WorkingDirectory: Path.GetTempPath(),
            ProjectDirectory: project.Path));

        Assert.True(result.Succeeded);
        Assert.Equal(catalogPath, result.Source);
        Assert.Equal("fabricator.json:local", result.SourceDescription);
    }

    [Fact]
    public void TemplatesValidateReportsActionableDiagnosticsForBrokenLocalCatalog()
    {
        using var workspace = new TemporaryDirectory();
        var source = Path.Combine(workspace.Path, "catalog.fabricator.json");
        File.WriteAllText(
            source,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-template-catalog",
              "displayName": "Broken catalog",
              "description": "Broken catalog.",
              "templates": [
                {
                  "id": "missing-template",
                  "displayName": "Missing Template",
                  "description": "Missing manifest.",
                  "version": "0.1.0",
                  "manifest": "missing-template/fabricator-template.json",
                  "tags": ["test"]
                }
              ]
            }
            """);
        var rootCommand = CliCommandFactory.CreateRootCommand();

        using var output = ConsoleOutputScope.Capture();
        var exitCode = rootCommand.Parse(["templates", "validate", "--source", source]).Invoke();

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Contains("Result: invalid", output.ToString());
        Assert.Contains("template-read-failed", output.ToString());
        Assert.Contains("missing-template", output.ToString());
        Assert.Contains("Next:", output.ToString());
    }

    [Fact]
    public void TemplatesApplyTracksStateAndDuplicateApplyDoesNotDuplicateExports()
    {
        using var project = CreateCompatibleProject();
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var rootCommand = CliCommandFactory.CreateRootCommand();

        using (ConsoleOutputScope.Capture())
        {
            var firstExitCode = rootCommand.Parse(
                ["templates", "apply", "component/primary-button", "--source", source, "--output", project.Path]).Invoke();
            Assert.Equal(ExitCodes.Success, firstExitCode);
        }

        using var output = ConsoleOutputScope.Capture();
        var secondExitCode = rootCommand.Parse(
            ["templates", "apply", "component/primary-button", "--source", source, "--output", project.Path]).Invoke();

        Assert.Equal(ExitCodes.Success, secondExitCode);
        Assert.Contains("Skipped:", output.ToString());
        Assert.Contains("Exports skipped: 1", output.ToString());
        Assert.Equal(
            1,
            CountOccurrences(
                File.ReadAllText(Path.Combine(project.Path, "src", "components", "index.ts")),
                "export { PrimaryButton } from './PrimaryButton';"));

        using var state = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(project.Path, FabricatorProjectStateContract.StateRelativePath)));
        var appliedTemplates = state.RootElement.GetProperty("appliedTemplates").EnumerateArray().ToArray();
        Assert.Equal(3, appliedTemplates.Length);
        Assert.All(
            appliedTemplates.Skip(1),
            item =>
            {
                Assert.Equal("component/primary-button", item.GetProperty("id").GetString());
                Assert.Equal("apply", item.GetProperty("operation").GetString());
                Assert.Equal("applied", item.GetProperty("result").GetString());
            });
    }

    [Fact]
    public void TemplateLifecycleDryRunsDoNotMutateLocalCatalogOrTemplateFiles()
    {
        using var project = CreateCompatibleProject();
        using var templates = new TemporaryDirectory();
        var catalogPath = Path.Combine(templates.Path, "catalog.fabricator.json");
        var profilePath = Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx");
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'old'; }\n");
        var rootCommand = CliCommandFactory.CreateRootCommand();

        using (ConsoleOutputScope.Capture())
        {
            var addDryRunExitCode = rootCommand.Parse([
                "templates",
                "add",
                "profile-screen",
                "--category",
                "screens",
                "--from",
                project.Path,
                "--source",
                catalogPath,
                "--dry-run"
            ]).Invoke();

            Assert.Equal(ExitCodes.Success, addDryRunExitCode);
            Assert.False(File.Exists(catalogPath));
            Assert.False(Directory.Exists(Path.Combine(templates.Path, "profile-screen")));
        }

        using (ConsoleOutputScope.Capture())
        {
            var addExitCode = rootCommand.Parse([
                "templates",
                "add",
                "profile-screen",
                "--category",
                "screens",
                "--from",
                project.Path,
                "--source",
                catalogPath
            ]).Invoke();

            Assert.Equal(ExitCodes.Success, addExitCode);
        }

        var originalCatalog = File.ReadAllText(catalogPath);
        var capturedProfilePath = Path.Combine(templates.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx");
        var originalCapturedProfile = File.ReadAllText(capturedProfilePath);
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'new'; }\n");

        using (ConsoleOutputScope.Capture())
        {
            var updateDryRunExitCode = rootCommand.Parse([
                "templates",
                "update",
                "profile-screen",
                "--from",
                project.Path,
                "--source",
                catalogPath,
                "--dry-run"
            ]).Invoke();

            Assert.Equal(ExitCodes.Success, updateDryRunExitCode);
            Assert.Equal(originalCatalog, File.ReadAllText(catalogPath));
            Assert.Equal(originalCapturedProfile, File.ReadAllText(capturedProfilePath));
        }

        using (ConsoleOutputScope.Capture())
        {
            var removeDryRunExitCode = rootCommand.Parse([
                "templates",
                "remove",
                "profile-screen",
                "--source",
                catalogPath,
                "--delete-files",
                "--dry-run"
            ]).Invoke();

            Assert.Equal(ExitCodes.Success, removeDryRunExitCode);
            Assert.Equal(originalCatalog, File.ReadAllText(catalogPath));
            Assert.True(Directory.Exists(Path.Combine(templates.Path, "profile-screen")));
        }
    }

    [Fact]
    public void TemplateCommandsRejectMissingOrInvalidFabricatorState()
    {
        using var missingStateProject = CreateCompatibleProject();
        File.Delete(Path.Combine(missingStateProject.Path, FabricatorProjectStateContract.StateRelativePath));
        using var invalidStateProject = CreateCompatibleProject();
        File.WriteAllText(Path.Combine(invalidStateProject.Path, FabricatorProjectStateContract.StateRelativePath), "{}");
        var source = FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json"));
        var rootCommand = CliCommandFactory.CreateRootCommand();

        using (var output = ConsoleOutputScope.Capture())
        {
            var exitCode = rootCommand.Parse(
                ["templates", "apply", "component/primary-button", "--source", source, "--output", missingStateProject.Path]).Invoke();

            Assert.Equal(ExitCodes.GeneralFailure, exitCode);
            Assert.Contains("Fabricator project state file was not found", output.ErrorOutput);
            Assert.Contains("Next:", output.ErrorOutput);
        }

        using (var output = ConsoleOutputScope.Capture())
        {
            var exitCode = rootCommand.Parse(["templates", "status", "--project", invalidStateProject.Path]).Invoke();

            Assert.Equal(ExitCodes.GeneralFailure, exitCode);
            Assert.Contains("Fabricator project state schema is not supported", output.ErrorOutput);
            Assert.Contains("Next:", output.ErrorOutput);
        }
    }

    private static TemporaryDirectory CreateCompatibleProject()
    {
        var project = new TemporaryDirectory();
        var manifest = FabricatorProjectContract.CreateManifest(ProductInfo.Version);

        Directory.CreateDirectory(Path.Combine(project.Path, ".fabricator"));
        Directory.CreateDirectory(Path.Combine(project.Path, "src"));

        foreach (var folder in manifest.Folders)
        {
            Directory.CreateDirectory(Path.Combine(project.Path, folder.Path));
        }

        foreach (var integrationPoint in manifest.IntegrationPoints)
        {
            var path = Path.Combine(project.Path, integrationPoint.Path);
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, "export {};\n");
        }

        File.WriteAllText(
            Path.Combine(project.Path, FabricatorProjectContract.ManifestRelativePath),
            JsonSerializer.Serialize(manifest, JsonOptions));
        File.WriteAllText(
            Path.Combine(project.Path, FabricatorProjectStateContract.StateRelativePath),
            JsonSerializer.Serialize(
                FabricatorProjectStateContract.CreateInitialState(
                    "BetaApp",
                    ProductInfo.Version,
                    CreateProjectService.DefaultStarterId,
                    "0.1.0",
                    "starter",
                    null,
                    ["App.tsx"]),
                JsonOptions));

        return project;
    }

    private static string WriteLocalCatalog(string projectPath)
    {
        var catalogPath = Path.Combine(projectPath, "templates", "catalog.fabricator.json");
        Directory.CreateDirectory(Path.GetDirectoryName(catalogPath)!);
        File.WriteAllText(
            catalogPath,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-template-catalog",
              "displayName": "Local beta catalog",
              "description": "Local beta catalog.",
              "templates": []
            }
            """);

        return catalogPath;
    }

    private static void WriteTemplateSourceState(string projectPath, string source)
    {
        using var state = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(projectPath, FabricatorProjectStateContract.StateRelativePath)));
        var root = state.RootElement.Clone();

        using var stream = File.Create(Path.Combine(projectPath, FabricatorProjectStateContract.StateRelativePath));
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("templateSources"))
            {
                writer.WritePropertyName("templateSources");
                writer.WriteStartArray();
                writer.WriteStartObject();
                writer.WriteString("type", "local");
                writer.WriteString("name", "local");
                writer.WriteString("value", source);
                writer.WriteBoolean("isDefault", true);
                writer.WriteEndObject();
                writer.WriteEndArray();
                continue;
            }

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
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

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rn-fabricator-beta-tests-{Guid.NewGuid():N}");
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

    private sealed class EmptyEnvironmentVariables : Fabricator.Core.Environment.IEnvironmentVariables
    {
        public string? Get(string name)
        {
            return null;
        }
    }
}
