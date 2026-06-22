using Fabricator.Core;
using Fabricator.Core.Projects;
using System.Text.Json;

namespace Fabricator.Tests.Projects;

public sealed class FabricatorProjectCompatibilityValidatorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void ValidateReturnsCompatibleForFabricatorProject()
    {
        using var project = CreateCompatibleProject();
        var validator = new FabricatorProjectCompatibilityValidator();

        var result = validator.Validate(project.Path);

        Assert.True(result.IsCompatible);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Manifest);
        Assert.Equal("fabricator-react-native-project", result.Manifest.Kind);
        Assert.Equal(Path.GetFullPath(project.Path), result.ProjectDirectory);
    }

    [Fact]
    public void ValidateReturnsErrorWhenManifestIsMissing()
    {
        using var project = new TemporaryDirectory();
        var validator = new FabricatorProjectCompatibilityValidator();

        var result = validator.Validate(project.Path);

        Assert.False(result.IsCompatible);
        Assert.Null(result.Manifest);
        Assert.Contains("Fabricator project manifest was not found", Assert.Single(result.Errors));
    }

    [Fact]
    public void ValidateReturnsErrorWhenRequiredFolderIsMissing()
    {
        using var project = CreateCompatibleProject();
        Directory.Delete(Path.Combine(project.Path, "src", "screens"), recursive: true);
        var validator = new FabricatorProjectCompatibilityValidator();

        var result = validator.Validate(project.Path);

        Assert.False(result.IsCompatible);
        Assert.Contains(result.Errors, error => error.Contains("Required Fabricator folder was not found: src/screens"));
    }

    [Fact]
    public void ValidateReturnsErrorWhenManifestPathEscapesProjectRoot()
    {
        using var project = CreateCompatibleProject(
            manifest => manifest with
            {
                Folders =
                [
                    new FabricatorProjectFolder(
                        "screens",
                        "../outside",
                        "Invalid folder.")
                ]
            });
        var validator = new FabricatorProjectCompatibilityValidator();

        var result = validator.Validate(project.Path);

        Assert.False(result.IsCompatible);
        Assert.Contains(result.Errors, error => error.Contains("Path resolved outside the project root"));
    }

    [Fact]
    public void ValidateReturnsErrorWhenIntegrationPointFileIsMissing()
    {
        using var project = CreateCompatibleProject();
        File.Delete(Path.Combine(project.Path, "src", "screens", "index.ts"));
        var validator = new FabricatorProjectCompatibilityValidator();

        var result = validator.Validate(project.Path);

        Assert.False(result.IsCompatible);
        Assert.Contains(result.Errors, error => error.Contains("Required Fabricator integration point was not found: src/screens/index.ts"));
    }

    [Fact]
    public void ValidateReturnsErrorWhenIntegrationPointTypeIsUnsupported()
    {
        using var project = CreateCompatibleProject(
            manifest => manifest with
            {
                IntegrationPoints =
                [
                    new FabricatorProjectIntegrationPoint(
                        "navigationRegistry",
                        "src/app/navigation.ts",
                        "navigation-registry",
                        "Unsupported integration point.")
                ]
            },
            createFiles: projectPath =>
            {
                Directory.CreateDirectory(Path.Combine(projectPath, "src", "app"));
                File.WriteAllText(Path.Combine(projectPath, "src", "app", "navigation.ts"), "export {};\n");
            });
        var validator = new FabricatorProjectCompatibilityValidator();

        var result = validator.Validate(project.Path);

        Assert.False(result.IsCompatible);
        Assert.Contains(result.Errors, error => error.Contains("Unsupported Fabricator integration point type"));
    }

    [Fact]
    public void ValidateReturnsErrorWhenSchemaVersionIsUnsupported()
    {
        using var project = CreateCompatibleProject(
            manifest => manifest with
            {
                SchemaVersion = FabricatorProjectContract.CurrentSchemaVersion + 1
            });
        var validator = new FabricatorProjectCompatibilityValidator();

        var result = validator.Validate(project.Path);

        Assert.False(result.IsCompatible);
        Assert.Contains(result.Errors, error => error.Contains("Unsupported Fabricator project schema version"));
    }

    private static TemporaryDirectory CreateCompatibleProject(
        Func<FabricatorProjectManifest, FabricatorProjectManifest>? configureManifest = null,
        Action<string>? createFiles = null)
    {
        var project = new TemporaryDirectory();
        var manifest = FabricatorProjectContract.CreateManifest(ProductInfo.Version);
        manifest = configureManifest?.Invoke(manifest) ?? manifest;

        Directory.CreateDirectory(Path.Combine(project.Path, ".fabricator"));
        Directory.CreateDirectory(Path.Combine(project.Path, "src"));

        foreach (var folder in manifest.Folders ?? [])
        {
            if (!Path.IsPathRooted(folder.Path) && !folder.Path.StartsWith("..", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(Path.Combine(project.Path, folder.Path));
            }
        }

        foreach (var integrationPoint in manifest.IntegrationPoints ?? [])
        {
            if (Path.IsPathRooted(integrationPoint.Path) || integrationPoint.Path.StartsWith("..", StringComparison.Ordinal))
            {
                continue;
            }

            var path = Path.Combine(project.Path, integrationPoint.Path);
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, "export {};\n");
        }

        createFiles?.Invoke(project.Path);

        File.WriteAllText(
            Path.Combine(project.Path, FabricatorProjectContract.ManifestRelativePath),
            JsonSerializer.Serialize(manifest, JsonOptions));

        return project;
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
