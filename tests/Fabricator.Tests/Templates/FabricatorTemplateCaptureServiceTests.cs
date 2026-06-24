using Fabricator.Core;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using System.Text.Json;

namespace Fabricator.Tests.Templates;

public sealed class FabricatorTemplateCaptureServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Fact]
    public async Task CaptureAsyncCreatesTemplateFolderAndManifestFromProjectFolder()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "profile-screen",
            "screens",
            project.Path,
            output.Path));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Contains("src/screens/ProfileScreen.tsx", result.CapturedFiles);
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "fabricator-template.json")));
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx")));

        var manifest = JsonSerializer.Deserialize<FabricatorTemplateManifest>(
            File.ReadAllText(Path.Combine(output.Path, "profile-screen", "fabricator-template.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(manifest);
        Assert.Equal(2, manifest.SchemaVersion);
        Assert.Equal("fabricator-template", manifest.Kind);
        Assert.Equal("profile-screen", manifest.Id);
        Assert.Equal("screens", manifest.Category);
        Assert.Contains(manifest.Files, file =>
            file.Path == "src/screens/ProfileScreen.tsx" &&
            file.TargetPath == "src/screens/ProfileScreen.tsx" &&
            file.TargetFolder == "screens");
    }

    [Fact]
    public async Task CaptureAsyncCreatesTemplateThatCanBeAppliedToAnotherProject()
    {
        using var sourceProject = CreateCompatibleProject();
        using var targetProject = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(sourceProject.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        var captureService = new FabricatorTemplateCaptureService();

        var captureResult = await captureService.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "profile-screen",
            "screens",
            sourceProject.Path,
            output.Path));
        WriteCatalog(output.Path);

        var package = await new TemplateCatalogProvider()
            .GetTemplateAsync(Path.Combine(output.Path, "catalog.fabricator.json"), "profile-screen");
        var applyResult = await new FabricatorTemplateApplyService()
            .ApplyAsync(new FabricatorTemplateApplyRequest(
                package,
                targetProject.Path,
                OverwriteExistingFiles: false));

        Assert.True(captureResult.Succeeded);
        Assert.True(applyResult.Succeeded);
        Assert.True(File.Exists(Path.Combine(targetProject.Path, "src", "screens", "ProfileScreen.tsx")));
        Assert.Contains("ProfileScreen", File.ReadAllText(Path.Combine(targetProject.Path, "src", "screens", "ProfileScreen.tsx")));
    }

    [Fact]
    public async Task CaptureAsyncRejectsUnknownCategoryWithoutWritingTemplate()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "profile-auth",
            "auth",
            project.Path,
            output.Path));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Category must match", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(output.Path, "profile-auth")));
    }

    [Fact]
    public async Task CaptureAsyncRejectsExistingOutputWithoutOverwriting()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(output.Path, "profile-screen"));
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "profile-screen",
            "screens",
            project.Path,
            output.Path));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Template output already exists", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("../profile-screen")]
    [InlineData("screen//profile")]
    [InlineData("screen/../profile")]
    public async Task CaptureAsyncRejectsUnsafeTemplateIdsWithoutWritingTemplate(string templateId)
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            templateId,
            "screens",
            project.Path,
            output.Path));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Template id must be a relative path", StringComparison.Ordinal));
        Assert.Empty(Directory.EnumerateFileSystemEntries(output.Path));
    }

    [Fact]
    public async Task CaptureAsyncRejectsNonFabricatorProjectWithoutWritingTemplate()
    {
        using var project = new TemporaryDirectory();
        using var output = new TemporaryDirectory();
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "profile-screen",
            "screens",
            project.Path,
            output.Path));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Fabricator project manifest was not found", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(output.Path, "profile-screen")));
    }

    [Fact]
    public async Task CaptureAsyncRejectsEmptyCaptureFolderWithoutWritingTemplate()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "empty-utils",
            "utils",
            project.Path,
            output.Path));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("No files were found in Fabricator folder: src/utils", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(output.Path, "empty-utils")));
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

        return project;
    }

    private static void WriteCatalog(string outputPath)
    {
        File.WriteAllText(
            Path.Combine(outputPath, "catalog.fabricator.json"),
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-template-catalog",
              "displayName": "Captured template catalog",
              "description": "Captured template catalog.",
              "templates": [
                {
                  "id": "profile-screen",
                  "displayName": "Profile Screen",
                  "description": "Captured profile screen.",
                  "version": "0.1.0",
                  "category": "screens",
                  "manifest": "profile-screen/fabricator-template.json",
                  "tags": ["screens"]
                }
              ]
            }
            """);
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
