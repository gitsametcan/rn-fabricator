using Fabricator.Core;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using System.Text.Json;

namespace Fabricator.Tests.Projects;

public sealed class FabricatorProjectStateServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Fact]
    public async Task TrackApplyAsyncAppendsAppliedTemplateOperation()
    {
        using var project = CreateCompatibleProject(includeState: true);
        var package = CreatePackage();
        var applyResult = new FabricatorTemplateApplyResult(
            ["src/screens/ProfileScreen.tsx", "src/components/ProfileCard.tsx"],
            ["src/services/profileApi.ts"],
            ["src/components/ProfileCard.tsx"],
            ["screensBarrel: export { ProfileScreen } from './ProfileScreen';"],
            ["componentsBarrel: export { ProfileCard } from './ProfileCard';"],
            [new FabricatorTemplateIntegrationReport("manual", "Wire ProfileScreen into navigation.", "navigation")],
            []);
        var service = new FabricatorProjectStateService();

        var result = await service.TrackApplyAsync(
            new FabricatorTemplateApplyStateTrackingRequest(
                project.Path,
                "./templates/catalog.fabricator.json",
                package,
                applyResult));

        Assert.True(result.Succeeded);

        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(project.Path, FabricatorProjectStateContract.StateRelativePath)));
        var appliedTemplates = document.RootElement.GetProperty("appliedTemplates").EnumerateArray().ToArray();
        Assert.Equal(2, appliedTemplates.Length);

        var operation = appliedTemplates[1];
        Assert.Equal("profile", operation.GetProperty("id").GetString());
        Assert.Equal("1.2.3", operation.GetProperty("version").GetString());
        Assert.Equal("screen", operation.GetProperty("category").GetString());
        Assert.Equal("apply", operation.GetProperty("operation").GetString());
        Assert.Equal("applied", operation.GetProperty("result").GetString());
        Assert.Equal("local", operation.GetProperty("source").GetProperty("type").GetString());
        Assert.Equal("./templates/catalog.fabricator.json", operation.GetProperty("source").GetProperty("value").GetString());

        var files = operation.GetProperty("files");
        Assert.Contains(files.GetProperty("written").EnumerateArray(), file => file.GetString() == "src/screens/ProfileScreen.tsx");
        Assert.Contains(files.GetProperty("skipped").EnumerateArray(), file => file.GetString() == "src/services/profileApi.ts");
        Assert.Contains(files.GetProperty("overwritten").EnumerateArray(), file => file.GetString() == "src/components/ProfileCard.tsx");

        var exports = operation.GetProperty("exports").EnumerateArray().ToArray();
        Assert.Contains(
            exports,
            item => item.GetProperty("integrationPoint").GetString() == "screensBarrel" &&
                    item.GetProperty("path").GetString() == "src/screens/index.ts" &&
                    item.GetProperty("result").GetString() == "added");
        Assert.Contains(
            exports,
            item => item.GetProperty("integrationPoint").GetString() == "componentsBarrel" &&
                    item.GetProperty("path").GetString() == "src/components/index.ts" &&
                    item.GetProperty("result").GetString() == "already-exists");

        var note = Assert.Single(operation.GetProperty("integrationNotes").EnumerateArray());
        Assert.Equal("manual", note.GetProperty("type").GetString());
        Assert.Equal("navigation", note.GetProperty("target").GetString());
    }

    [Fact]
    public async Task GetTemplateStatusAsyncReturnsAppliedTemplateStatusWithLocalCatalogComparison()
    {
        using var project = CreateCompatibleProject(includeState: true);
        var package = CreatePackage();
        WriteCatalog(project.Path, "profile", "1.2.3");
        var service = new FabricatorProjectStateService();

        var update = await service.TrackApplyAsync(
            new FabricatorTemplateApplyStateTrackingRequest(
                project.Path,
                "./templates/catalog.fabricator.json",
                package,
                new FabricatorTemplateApplyResult(
                    ["src/screens/ProfileScreen.tsx"],
                    [],
                    [],
                    ["screensBarrel: export { ProfileScreen } from './ProfileScreen';"],
                    [],
                    [],
                    [])));

        var status = await service.GetTemplateStatusAsync(project.Path);

        Assert.True(update.Succeeded);
        Assert.True(status.Succeeded);
        Assert.Equal(project.Path, status.ProjectDirectory);

        var profile = Assert.Single(status.AppliedTemplates, template => template.Id == "profile");
        Assert.Equal("1.2.3", profile.Version);
        Assert.Equal("screen", profile.Category);
        Assert.Equal("apply", profile.Operation);
        Assert.Equal("applied", profile.Result);
        Assert.Equal("local", profile.Source.Type);
        Assert.Equal("current", profile.CatalogStatus);
        Assert.Equal("1.2.3", profile.CatalogVersion);
        Assert.EndsWith(
            Path.Combine("templates", "catalog.fabricator.json"),
            profile.CatalogSource,
            StringComparison.Ordinal);
        Assert.Single(profile.Files.Written);
        Assert.Equal(1, profile.ExportCount);
    }

    [Fact]
    public async Task GetTemplateStatusAsyncRequiresFabricatorStateFile()
    {
        using var project = CreateCompatibleProject(includeState: false);
        var service = new FabricatorProjectStateService();

        var status = await service.GetTemplateStatusAsync(project.Path);

        Assert.False(status.Succeeded);
        Assert.Contains(
            status.Errors,
            error => error.Contains("Fabricator project state file was not found", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateCanTrackReturnsCompatibilityErrorsBeforeStateErrors()
    {
        using var project = new TemporaryDirectory();
        var service = new FabricatorProjectStateService();

        var result = service.ValidateCanTrack(project.Path);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.Contains("Fabricator project manifest was not found", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateCanTrackRequiresFabricatorStateFile()
    {
        using var project = CreateCompatibleProject(includeState: false);
        var service = new FabricatorProjectStateService();

        var result = service.ValidateCanTrack(project.Path);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.Contains("Fabricator project state file was not found", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateCanTrackRejectsInvalidFabricatorStateFile()
    {
        using var project = CreateCompatibleProject(includeState: true);
        File.WriteAllText(Path.Combine(project.Path, FabricatorProjectStateContract.StateRelativePath), "{}");
        var service = new FabricatorProjectStateService();

        var result = service.ValidateCanTrack(project.Path);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.Contains("state schema is not supported", StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains("appliedTemplates array", StringComparison.Ordinal));
    }

    private static FabricatorTemplatePackage CreatePackage()
    {
        var files = new[]
        {
            new FabricatorTemplateFile(
                "ProfileScreen.tsx",
                "file",
                "src/screens/ProfileScreen.tsx",
                "screens",
                "Profile screen."),
            new FabricatorTemplateFile(
                "ProfileCard.tsx",
                "file",
                "src/components/ProfileCard.tsx",
                "components",
                "Profile card.")
        };

        return new FabricatorTemplatePackage(
            new FabricatorTemplateManifest(
                2,
                "fabricator-template",
                "profile",
                "Profile",
                "Profile feature.",
                "1.2.3",
                "apply",
                files,
                Category: "screen"),
            files.ToDictionary(file => file.Path, _ => "content\n"));
    }

    private static void WriteCatalog(string projectPath, string templateId, string version)
    {
        var templatesPath = Path.Combine(projectPath, "templates");
        Directory.CreateDirectory(templatesPath);
        File.WriteAllText(
            Path.Combine(templatesPath, "catalog.fabricator.json"),
            $$"""
            {
              "schemaVersion": 1,
              "kind": "fabricator-template-catalog",
              "displayName": "Test catalog",
              "description": "Test catalog.",
              "templates": [
                {
                  "id": "{{templateId}}",
                  "displayName": "Profile",
                  "description": "Profile template.",
                  "version": "{{version}}",
                  "manifest": "profile/fabricator-template.json",
                  "tags": ["profile"],
                  "category": "screen"
                }
              ]
            }
            """);
    }

    private static TemporaryDirectory CreateCompatibleProject(bool includeState)
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

        if (includeState)
        {
            File.WriteAllText(
                Path.Combine(project.Path, FabricatorProjectStateContract.StateRelativePath),
                JsonSerializer.Serialize(
                    FabricatorProjectStateContract.CreateInitialState(
                        "TestApp",
                        ProductInfo.Version,
                        CreateProjectService.DefaultStarterId,
                        "0.1.0",
                        "starter",
                        null,
                        ["App.tsx"]),
                    JsonOptions));
        }

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
