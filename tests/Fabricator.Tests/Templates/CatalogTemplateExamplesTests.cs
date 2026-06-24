using Fabricator.Core;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using System.Text.Json;

namespace Fabricator.Tests.Templates;

public sealed class CatalogTemplateExamplesTests
{
    private static readonly string[] ExampleTemplateIds =
    [
        "screen/main-menu",
        "service/api-client",
        "util/storage",
        "layout/app-shell",
        "component/primary-button"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Fact]
    public async Task RepositoryCatalogListsReusableV090Examples()
    {
        var provider = new TemplateCatalogProvider();

        var catalog = await provider.ListTemplatesAsync(FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json")));

        foreach (var templateId in ExampleTemplateIds)
        {
            Assert.Contains(catalog.Templates, template => template.Id == templateId);
        }

        Assert.Contains(catalog.Templates, template => template.Id == "screen/main-menu" && template.Category == "screen");
        Assert.Contains(catalog.Templates, template => template.Id == "service/api-client" && template.Category == "service");
        Assert.Contains(catalog.Templates, template => template.Id == "util/storage" && template.Category == "util");
        Assert.Contains(catalog.Templates, template => template.Id == "layout/app-shell" && template.Category == "layout");
        Assert.Contains(catalog.Templates, template => template.Id == "component/primary-button" && template.Category == "component");
    }

    [Theory]
    [InlineData("screen/main-menu", "screen", "src/screens/MainMenuScreen.tsx", "screens")]
    [InlineData("service/api-client", "service", "src/services/apiClient.ts", "services")]
    [InlineData("util/storage", "util", "src/utils/storage.ts", "utils")]
    [InlineData("layout/app-shell", "layout", "src/app/AppShell.tsx", "app")]
    [InlineData("component/primary-button", "component", "src/components/PrimaryButton.tsx", "components")]
    public async Task RepositoryExamplesUseSchemaV2Metadata(
        string templateId,
        string category,
        string expectedFile,
        string expectedTargetFolder)
    {
        var provider = new TemplateCatalogProvider();

        var package = await provider.GetTemplateAsync(
            FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json")),
            templateId);

        Assert.Equal(2, package.Manifest.SchemaVersion);
        Assert.Equal("fabricator-template", package.Manifest.Kind);
        Assert.Equal(templateId, package.Manifest.Id);
        Assert.Equal("apply", package.Manifest.Mode);
        Assert.Equal(category, package.Manifest.Category);
        Assert.Contains(category, package.Manifest.Tags ?? []);
        Assert.Contains(package.Manifest.Files, file =>
            file.Path == expectedFile &&
            file.TargetPath == expectedFile &&
            file.TargetFolder == expectedTargetFolder);
        Assert.Contains(expectedFile, package.Files.Keys);
    }

    [Theory]
    [MemberData(nameof(ExampleTemplateIdsData))]
    public async Task RepositoryExamplesApplyToCompatibleFabricatorProject(string templateId)
    {
        var provider = new TemplateCatalogProvider();
        var package = await provider.GetTemplateAsync(
            FindRepositoryFile(Path.Combine("templates", "catalog.fabricator.json")),
            templateId);
        using var project = CreateCompatibleProject();
        var service = new FabricatorTemplateApplyService();

        var result = await service.ApplyAsync(new FabricatorTemplateApplyRequest(
            package,
            project.Path,
            OverwriteExistingFiles: false));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        foreach (var file in package.Manifest.Files)
        {
            var targetPath = file.TargetPath ?? file.Path;
            Assert.True(
                File.Exists(Path.Combine(project.Path, targetPath)),
                $"Expected applied file was missing: {targetPath}");
        }
    }

    public static IEnumerable<object[]> ExampleTemplateIdsData() =>
        ExampleTemplateIds.Select(templateId => new object[] { templateId });

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
