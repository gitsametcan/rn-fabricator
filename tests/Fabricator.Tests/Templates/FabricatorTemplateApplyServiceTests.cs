using Fabricator.Core;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using System.Text.Json;

namespace Fabricator.Tests.Templates;

public sealed class FabricatorTemplateApplyServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Fact]
    public async Task ApplyAsyncWritesTemplateFilesToFabricatorProjectTargets()
    {
        using var project = CreateCompatibleProject();
        var package = CreatePackage();
        var service = new FabricatorTemplateApplyService();

        var result = await service.ApplyAsync(new FabricatorTemplateApplyRequest(
            package,
            project.Path,
            OverwriteExistingFiles: false));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.SkippedFiles);
        Assert.Contains("src/screens/ProfileScreen.tsx", result.GeneratedFiles);
        Assert.Contains("src/services/profileApi.ts", result.GeneratedFiles);
        Assert.Equal(
            "export function ProfileScreen() { return null; }\n",
            File.ReadAllText(Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx")));
    }

    [Fact]
    public async Task ApplyAsyncSkipsExistingFilesByDefault()
    {
        using var project = CreateCompatibleProject();
        var existingPath = Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx");
        File.WriteAllText(existingPath, "existing screen\n");
        var service = new FabricatorTemplateApplyService();

        var result = await service.ApplyAsync(new FabricatorTemplateApplyRequest(
            CreatePackage(),
            project.Path,
            OverwriteExistingFiles: false));

        Assert.True(result.Succeeded);
        Assert.Contains("src/screens/ProfileScreen.tsx", result.SkippedFiles);
        Assert.Equal("existing screen\n", File.ReadAllText(existingPath));
    }

    [Fact]
    public async Task ApplyAsyncOverwritesExistingFilesWhenRequested()
    {
        using var project = CreateCompatibleProject();
        var existingPath = Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx");
        File.WriteAllText(existingPath, "existing screen\n");
        var service = new FabricatorTemplateApplyService();

        var result = await service.ApplyAsync(new FabricatorTemplateApplyRequest(
            CreatePackage(),
            project.Path,
            OverwriteExistingFiles: true));

        Assert.True(result.Succeeded);
        Assert.Empty(result.SkippedFiles);
        Assert.Equal(
            "export function ProfileScreen() { return null; }\n",
            File.ReadAllText(existingPath));
    }

    [Fact]
    public async Task ApplyAsyncReturnsCompatibilityErrorsForNonFabricatorProject()
    {
        using var project = new TemporaryDirectory();
        var service = new FabricatorTemplateApplyService();

        var result = await service.ApplyAsync(new FabricatorTemplateApplyRequest(
            CreatePackage(),
            project.Path,
            OverwriteExistingFiles: false));

        Assert.False(result.Succeeded);
        Assert.Empty(result.GeneratedFiles);
        Assert.Contains(result.Errors, error => error.Contains("Fabricator project manifest was not found", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(project.Path, "src", "screens", "ProfileScreen.tsx")));
    }

    [Fact]
    public async Task ApplyAsyncRejectsTargetPathsOutsideProjectRoot()
    {
        using var project = CreateCompatibleProject();
        var package = CreatePackage(
            files:
            [
                new FabricatorTemplateFile(
                    "screen.tsx",
                    "source",
                    "../outside.tsx",
                    "screens",
                    "Unsafe screen.")
            ]);
        var service = new FabricatorTemplateApplyService();

        var result = await service.ApplyAsync(new FabricatorTemplateApplyRequest(
            package,
            project.Path,
            OverwriteExistingFiles: false));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Target path resolved outside the project root", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(project.Path, "..", "outside.tsx")));
    }

    [Fact]
    public async Task ApplyAsyncRejectsUnknownTargetFolders()
    {
        using var project = CreateCompatibleProject();
        var package = CreatePackage(
            files:
            [
                new FabricatorTemplateFile(
                    "unknown.ts",
                    "source",
                    "src/unknown/unknown.ts",
                    "unknown",
                    "Unknown folder.")
            ]);
        var service = new FabricatorTemplateApplyService();

        var result = await service.ApplyAsync(new FabricatorTemplateApplyRequest(
            package,
            project.Path,
            OverwriteExistingFiles: false));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("targets unknown Fabricator folder: unknown", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(project.Path, "src", "unknown", "unknown.ts")));
    }

    private static FabricatorTemplatePackage CreatePackage(IReadOnlyList<FabricatorTemplateFile>? files = null)
    {
        files ??=
        [
            new FabricatorTemplateFile(
                "screen.tsx",
                "source",
                "src/screens/ProfileScreen.tsx",
                "screens",
                "Profile screen."),
            new FabricatorTemplateFile(
                "api.ts",
                "source",
                "src/services/profileApi.ts",
                "services",
                "Profile API service.")
        ];

        return new FabricatorTemplatePackage(
            new FabricatorTemplateManifest(
                2,
                "fabricator-template",
                "profile",
                "Profile",
                "Profile feature.",
                "1.0.0",
                "copy",
                files,
                Category: "screen"),
            files.ToDictionary(
                file => file.Path,
                file => file.Path.EndsWith("screen.tsx", StringComparison.Ordinal)
                    ? "export function ProfileScreen() { return null; }\n"
                    : "export async function getProfile() { return {}; }\n"));
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
