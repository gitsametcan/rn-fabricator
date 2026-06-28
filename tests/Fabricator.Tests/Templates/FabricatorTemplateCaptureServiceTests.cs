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
    public async Task CaptureAsyncCreatesTemplateFromIncludedFilesOnly()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "SettingsScreen.tsx"),
            "export function SettingsScreen() { return null; }\n");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "components", "PrimaryButton.tsx"),
            "export function PrimaryButton() { return null; }\n");
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "profile-screen",
            "screens",
            project.Path,
            output.Path,
            IncludePaths:
            [
                "src/screens/ProfileScreen.tsx",
                "src/components/PrimaryButton.tsx"
            ]));

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["src/components/PrimaryButton.tsx", "src/screens/ProfileScreen.tsx"],
            result.CapturedFiles.Order(StringComparer.Ordinal));
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx")));
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "components", "PrimaryButton.tsx")));
        Assert.False(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "SettingsScreen.tsx")));

        var manifest = JsonSerializer.Deserialize<FabricatorTemplateManifest>(
            File.ReadAllText(Path.Combine(output.Path, "profile-screen", "fabricator-template.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(manifest);
        Assert.Contains(manifest.Files, file =>
            file.Path == "src/screens/ProfileScreen.tsx" &&
            file.TargetPath == "src/screens/ProfileScreen.tsx" &&
            file.TargetFolder == "screens");
        Assert.Contains(manifest.Files, file =>
            file.Path == "src/components/PrimaryButton.tsx" &&
            file.TargetPath == "src/components/PrimaryButton.tsx" &&
            file.TargetFolder == "components");
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
    public async Task CaptureAsyncCreatesIncludedTemplateThatCanBeAppliedToAnotherProject()
    {
        using var sourceProject = CreateCompatibleProject();
        using var targetProject = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(sourceProject.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        File.WriteAllText(
            Path.Combine(sourceProject.Path, "src", "components", "PrimaryButton.tsx"),
            "export function PrimaryButton() { return null; }\n");
        var captureService = new FabricatorTemplateCaptureService();

        var captureResult = await captureService.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "profile-screen",
            "screens",
            sourceProject.Path,
            output.Path,
            IncludePaths:
            [
                "src/screens/ProfileScreen.tsx",
                "src/components/PrimaryButton.tsx"
            ]));
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
        Assert.True(File.Exists(Path.Combine(targetProject.Path, "src", "components", "PrimaryButton.tsx")));
    }

    [Fact]
    public async Task AddAsyncCapturesTemplateAndRegistersCatalogEntry()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        var service = new FabricatorTemplateAddService();

        var result = await service.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Equal(catalogPath, result.CatalogPath);
        Assert.True(File.Exists(catalogPath));
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "fabricator-template.json")));
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx")));

        var package = await new TemplateCatalogProvider().GetTemplateAsync(catalogPath, "profile-screen");
        Assert.Equal("profile-screen", package.Manifest.Id);
        Assert.Equal("screens", package.Manifest.Category);
    }

    [Fact]
    public async Task AddAsyncCapturesIncludedFilesAndRegistersCatalogEntry()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "SettingsScreen.tsx"),
            "export function SettingsScreen() { return null; }\n");
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        var service = new FabricatorTemplateAddService();

        var result = await service.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath,
            IncludePaths: ["src/screens/ProfileScreen.tsx"]));

        Assert.True(result.Succeeded);
        Assert.Equal(["src/screens/ProfileScreen.tsx"], result.CapturedFiles);
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx")));
        Assert.False(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "SettingsScreen.tsx")));

        var package = await new TemplateCatalogProvider().GetTemplateAsync(catalogPath, "profile-screen");
        Assert.Single(package.Manifest.Files);
        Assert.Equal("src/screens/ProfileScreen.tsx", package.Manifest.Files[0].TargetPath);
    }

    [Fact]
    public async Task AddAsyncCapturesNavigationTemplateFromIncludedFiles()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var navigationDirectory = Path.Combine(project.Path, "src", "navigation");
        File.WriteAllText(
            Path.Combine(navigationDirectory, "AppNavigator.tsx"),
            "export function AppNavigator() { return null; }\n");
        File.WriteAllText(
            Path.Combine(navigationDirectory, "routes.ts"),
            "export const routes = {};\n");
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        var service = new FabricatorTemplateAddService();

        var result = await service.AddAsync(new FabricatorTemplateAddRequest(
            "navigation/app-navigator",
            "navigation",
            project.Path,
            catalogPath,
            IncludePaths:
            [
                "src/navigation/AppNavigator.tsx",
                "src/navigation/routes.ts"
            ]));

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["src/navigation/AppNavigator.tsx", "src/navigation/routes.ts"],
            result.CapturedFiles);
        Assert.True(File.Exists(Path.Combine(output.Path, "navigation", "app-navigator", "src", "navigation", "AppNavigator.tsx")));
        Assert.True(File.Exists(Path.Combine(output.Path, "navigation", "app-navigator", "src", "navigation", "routes.ts")));

        var package = await new TemplateCatalogProvider().GetTemplateAsync(catalogPath, "navigation/app-navigator");
        Assert.Equal("navigation", package.Manifest.Category);
        Assert.Contains(package.Manifest.Files, file =>
            file.Path == "src/navigation/AppNavigator.tsx" &&
            file.TargetPath == "src/navigation/AppNavigator.tsx" &&
            file.TargetFolder == "navigation");
    }

    [Fact]
    public async Task AddAsyncRejectsDuplicateTemplateIdWithoutWritingTemplate()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        WriteCatalog(output.Path);
        var service = new FabricatorTemplateAddService();

        var result = await service.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            Path.Combine(output.Path, "catalog.fabricator.json")));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Template id already exists", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(output.Path, "profile-screen")));
    }

    [Fact]
    public async Task AddAsyncDryRunReportsCapturedFilesWithoutWritingTemplateOrCatalog()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        var service = new FabricatorTemplateAddService();

        var result = await service.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath,
            DryRun: true));

        Assert.True(result.Succeeded);
        Assert.True(result.DryRun);
        Assert.Contains("src/screens/ProfileScreen.tsx", result.CapturedFiles);
        Assert.False(File.Exists(catalogPath));
        Assert.False(Directory.Exists(Path.Combine(output.Path, "profile-screen")));
    }

    [Fact]
    public async Task UpdateAsyncRefreshesExistingTemplateAndReportsFileChanges()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        var profilePath = Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx");
        var keepPath = Path.Combine(project.Path, "src", "screens", "KeepScreen.tsx");
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'old'; }\n");
        File.WriteAllText(keepPath, "export function KeepScreen() { return null; }\n");
        var addService = new FabricatorTemplateAddService();
        var updateService = new FabricatorTemplateUpdateService();

        var addResult = await addService.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath));
        PreserveCustomManifestMetadata(Path.Combine(output.Path, "profile-screen", "fabricator-template.json"));
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'new'; }\n");
        File.Delete(keepPath);
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "SettingsScreen.tsx"),
            "export function SettingsScreen() { return null; }\n");

        var result = await updateService.UpdateAsync(new FabricatorTemplateUpdateRequest(
            "profile-screen",
            project.Path,
            catalogPath));

        Assert.True(addResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.Contains("src/screens/SettingsScreen.tsx", result.AddedFiles);
        Assert.Contains("src/screens/ProfileScreen.tsx", result.ChangedFiles);
        Assert.Contains("src/screens/KeepScreen.tsx", result.RemovedFiles);
        Assert.Contains("src/screens/index.ts", result.UnchangedFiles);
        Assert.Contains("displayName", result.PreservedMetadata);
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "SettingsScreen.tsx")));
        Assert.False(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "KeepScreen.tsx")));
        Assert.Contains(
            "return 'new'",
            File.ReadAllText(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx")));

        var manifest = JsonSerializer.Deserialize<FabricatorTemplateManifest>(
            File.ReadAllText(Path.Combine(output.Path, "profile-screen", "fabricator-template.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(manifest);
        Assert.Equal("Custom Profile Template", manifest.DisplayName);
        Assert.Equal("1.2.3", manifest.Version);
        Assert.Contains("custom", manifest.Tags ?? []);
    }

    [Fact]
    public async Task UpdateAsyncRefreshesIncludedFilesOnly()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        var profilePath = Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx");
        var settingsPath = Path.Combine(project.Path, "src", "screens", "SettingsScreen.tsx");
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'old'; }\n");
        File.WriteAllText(settingsPath, "export function SettingsScreen() { return 'old'; }\n");
        var addService = new FabricatorTemplateAddService();
        var updateService = new FabricatorTemplateUpdateService();

        var addResult = await addService.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath,
            IncludePaths: ["src/screens/ProfileScreen.tsx"]));
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'new'; }\n");
        File.WriteAllText(settingsPath, "export function SettingsScreen() { return 'new'; }\n");

        var result = await updateService.UpdateAsync(new FabricatorTemplateUpdateRequest(
            "profile-screen",
            project.Path,
            catalogPath,
            IncludePaths: ["src/screens/ProfileScreen.tsx"]));

        Assert.True(addResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.Contains("src/screens/ProfileScreen.tsx", result.ChangedFiles);
        Assert.DoesNotContain("src/screens/SettingsScreen.tsx", result.AddedFiles);
        Assert.True(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx")));
        Assert.False(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "SettingsScreen.tsx")));

        var manifest = JsonSerializer.Deserialize<FabricatorTemplateManifest>(
            File.ReadAllText(Path.Combine(output.Path, "profile-screen", "fabricator-template.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(manifest);
        Assert.Single(manifest.Files);
        Assert.Equal("src/screens/ProfileScreen.tsx", manifest.Files[0].Path);
    }

    [Fact]
    public async Task UpdateAsyncRejectsMissingTemplateIdWithoutWritingTemplate()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        WriteCatalog(output.Path);
        var updateService = new FabricatorTemplateUpdateService();

        var result = await updateService.UpdateAsync(new FabricatorTemplateUpdateRequest(
            "missing-template",
            project.Path,
            Path.Combine(output.Path, "catalog.fabricator.json")));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Template id was not found in catalog", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(output.Path, "missing-template")));
    }

    [Fact]
    public async Task UpdateAsyncDryRunReportsChangesWithoutWritingTemplateOrCatalog()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        var profilePath = Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx");
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'old'; }\n");
        var addService = new FabricatorTemplateAddService();
        var updateService = new FabricatorTemplateUpdateService();

        var addResult = await addService.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath));
        var originalCatalog = File.ReadAllText(catalogPath);
        var originalTemplate = File.ReadAllText(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx"));
        File.WriteAllText(profilePath, "export function ProfileScreen() { return 'new'; }\n");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "SettingsScreen.tsx"),
            "export function SettingsScreen() { return null; }\n");

        var result = await updateService.UpdateAsync(new FabricatorTemplateUpdateRequest(
            "profile-screen",
            project.Path,
            catalogPath,
            DryRun: true));

        Assert.True(addResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.True(result.DryRun);
        Assert.Contains("src/screens/ProfileScreen.tsx", result.ChangedFiles);
        Assert.Contains("src/screens/SettingsScreen.tsx", result.AddedFiles);
        Assert.Equal(originalCatalog, File.ReadAllText(catalogPath));
        Assert.Equal(originalTemplate, File.ReadAllText(Path.Combine(output.Path, "profile-screen", "src", "screens", "ProfileScreen.tsx")));
        Assert.False(File.Exists(Path.Combine(output.Path, "profile-screen", "src", "screens", "SettingsScreen.tsx")));
    }

    [Fact]
    public async Task RemoveAsyncRemovesCatalogEntryAndKeepsTemplateFilesByDefault()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        var addService = new FabricatorTemplateAddService();
        var removeService = new FabricatorTemplateRemoveService();

        var addResult = await addService.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath));

        var result = await removeService.RemoveAsync(new FabricatorTemplateRemoveRequest(
            "profile-screen",
            catalogPath,
            DeleteFiles: false));

        Assert.True(addResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.True(result.CatalogEntryRemoved);
        Assert.False(result.TemplateFilesDeleted);
        Assert.True(Directory.Exists(Path.Combine(output.Path, "profile-screen")));

        using var document = JsonDocument.Parse(File.ReadAllText(catalogPath));
        Assert.Empty(document.RootElement.GetProperty("templates").EnumerateArray());
    }

    [Fact]
    public async Task RemoveAsyncDeletesTemplateFilesOnlyWhenRequested()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        var addService = new FabricatorTemplateAddService();
        var removeService = new FabricatorTemplateRemoveService();

        var addResult = await addService.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath));

        var result = await removeService.RemoveAsync(new FabricatorTemplateRemoveRequest(
            "profile-screen",
            catalogPath,
            DeleteFiles: true));

        Assert.True(addResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.True(result.CatalogEntryRemoved);
        Assert.True(result.TemplateFilesDeleted);
        Assert.False(Directory.Exists(Path.Combine(output.Path, "profile-screen")));
    }

    [Fact]
    public async Task RemoveAsyncRejectsMissingTemplateIdWithoutChangingCatalog()
    {
        using var output = new TemporaryDirectory();
        WriteCatalog(output.Path);
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        var originalCatalog = File.ReadAllText(catalogPath);
        var removeService = new FabricatorTemplateRemoveService();

        var result = await removeService.RemoveAsync(new FabricatorTemplateRemoveRequest(
            "missing-template",
            catalogPath,
            DeleteFiles: false));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Template id was not found in catalog", StringComparison.Ordinal));
        Assert.Equal(originalCatalog, File.ReadAllText(catalogPath));
    }

    [Fact]
    public async Task RemoveAsyncRejectsUnsafeFileDeletionWithoutChangingCatalog()
    {
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        File.WriteAllText(
            catalogPath,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-template-catalog",
              "displayName": "Unsafe catalog",
              "description": "Unsafe catalog.",
              "templates": [
                {
                  "id": "unsafe-template",
                  "displayName": "Unsafe",
                  "description": "Unsafe.",
                  "version": "0.1.0",
                  "category": "screens",
                  "manifest": "../unsafe/fabricator-template.json",
                  "tags": ["screens"]
                }
              ]
            }
            """);
        var originalCatalog = File.ReadAllText(catalogPath);
        var removeService = new FabricatorTemplateRemoveService();

        var result = await removeService.RemoveAsync(new FabricatorTemplateRemoveRequest(
            "unsafe-template",
            catalogPath,
            DeleteFiles: true));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("resolved outside the catalog directory", StringComparison.Ordinal));
        Assert.Equal(originalCatalog, File.ReadAllText(catalogPath));
    }

    [Fact]
    public async Task RemoveAsyncDryRunLeavesCatalogAndTemplateFilesUnchanged()
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        var catalogPath = Path.Combine(output.Path, "catalog.fabricator.json");
        File.WriteAllText(
            Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx"),
            "export function ProfileScreen() { return null; }\n");
        var addService = new FabricatorTemplateAddService();
        var removeService = new FabricatorTemplateRemoveService();

        var addResult = await addService.AddAsync(new FabricatorTemplateAddRequest(
            "profile-screen",
            "screens",
            project.Path,
            catalogPath));
        var originalCatalog = File.ReadAllText(catalogPath);

        var result = await removeService.RemoveAsync(new FabricatorTemplateRemoveRequest(
            "profile-screen",
            catalogPath,
            DeleteFiles: true,
            DryRun: true));

        Assert.True(addResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.True(result.DryRun);
        Assert.False(result.CatalogEntryRemoved);
        Assert.False(result.TemplateFilesDeleted);
        Assert.Equal(originalCatalog, File.ReadAllText(catalogPath));
        Assert.True(Directory.Exists(Path.Combine(output.Path, "profile-screen")));
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

    [Theory]
    [InlineData("../outside.tsx", "must not contain current or parent segments")]
    [InlineData("src/screens/MissingScreen.tsx", "Included file was not found")]
    [InlineData("src/screens", "Included path must be a file")]
    [InlineData("App.tsx", "Included file must be under a Fabricator project folder")]
    public async Task CaptureAsyncRejectsInvalidIncludePathsWithoutWritingTemplate(
        string includePath,
        string expectedError)
    {
        using var project = CreateCompatibleProject();
        using var output = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(project.Path, "App.tsx"), "export {};\n");
        var service = new FabricatorTemplateCaptureService();

        var result = await service.CaptureAsync(new FabricatorTemplateCaptureRequest(
            "invalid-include",
            "screens",
            project.Path,
            output.Path,
            IncludePaths: [includePath]));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains(expectedError, StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(output.Path, "invalid-include")));
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

    private static void PreserveCustomManifestMetadata(string manifestPath)
    {
        var manifest = JsonSerializer.Deserialize<FabricatorTemplateManifest>(
            File.ReadAllText(manifestPath),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(manifest);

        var updated = manifest with
        {
            DisplayName = "Custom Profile Template",
            Description = "Custom description.",
            Version = "1.2.3",
            Tags = ["custom", "screens"]
        };

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(updated, JsonOptions));
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
