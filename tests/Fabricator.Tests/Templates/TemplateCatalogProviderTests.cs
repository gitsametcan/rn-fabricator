using Fabricator.Core.Templates;

namespace Fabricator.Tests.Templates;

public sealed class TemplateCatalogProviderTests
{
    [Fact]
    public async Task ListTemplatesAsyncReadsLocalCatalog()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path, mode: "starter");
        var provider = new TemplateCatalogProvider();

        var catalog = await provider.ListTemplatesAsync(catalogPath);

        Assert.Equal("fabricator-template-catalog", catalog.Kind);
        var template = Assert.Single(catalog.Templates);
        Assert.Equal("minimal-splash", template.Id);
        Assert.Equal("Minimal Splash", template.DisplayName);
        Assert.Equal("0.1.0", template.Version);
    }

    [Fact]
    public async Task GetTemplateAsyncReadsLocalCatalogManifestAndFiles()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path, mode: "starter");
        var provider = new TemplateCatalogProvider();

        var package = await provider.GetTemplateAsync(catalogPath, "minimal-splash");

        Assert.Equal("minimal-splash", package.Manifest.Id);
        Assert.Equal("starter", package.Manifest.Mode);
        Assert.Equal("export default function App() {}\n", package.Files["App.tsx"]);
        Assert.Equal("export {};\n", package.Files["src/screens/index.ts"]);
    }

    [Fact]
    public async Task GetTemplateAsyncThrowsWhenTemplateIsMissing()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path, mode: "starter");
        var provider = new TemplateCatalogProvider();

        var exception = await Assert.ThrowsAsync<TemplatePackageException>(
            () => provider.GetTemplateAsync(catalogPath, "missing-template"));

        Assert.Contains("missing-template", exception.Message);
    }

    private static string WriteCatalog(string root, string mode)
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

        File.WriteAllText(Path.Combine(templateRoot, "App.tsx"), "export default function App() {}\n");
        File.WriteAllText(Path.Combine(templateRoot, "src", "screens", "index.ts"), "export {};\n");

        return Path.Combine(root, "catalog.fabricator.json");
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
