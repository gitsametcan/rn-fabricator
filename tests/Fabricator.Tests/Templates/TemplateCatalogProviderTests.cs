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
    public async Task GetTemplateAsyncReadsSchemaV2Metadata()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteSchemaV2Catalog(directory.Path);
        var provider = new TemplateCatalogProvider();

        var catalog = await provider.ListTemplatesAsync(catalogPath);
        var package = await provider.GetTemplateAsync(catalogPath, "screen/main-menu");

        var entry = Assert.Single(catalog.Templates);
        Assert.Equal("screen", entry.Category);
        Assert.Equal(2, package.Manifest.SchemaVersion);
        Assert.Equal("screen", package.Manifest.Category);
        Assert.Contains("menu", package.Manifest.Tags ?? []);

        var file = Assert.Single(package.Manifest.Files);
        Assert.Equal("src/screens/MainMenuScreen.tsx", file.Path);
        Assert.Equal("screens", file.TargetFolder);
        Assert.Equal("src/screens/MainMenuScreen.tsx", file.TargetPath);

        var dependency = Assert.Single(package.Manifest.Dependencies ?? []);
        Assert.Equal("npm", dependency.Type);
        Assert.Equal("@react-navigation/native", dependency.Name);

        var export = Assert.Single(package.Manifest.Exports ?? []);
        Assert.Equal("screensBarrel", export.IntegrationPoint);
        Assert.Contains("MainMenuScreen", export.Statement);

        var hint = Assert.Single(package.Manifest.IntegrationHints ?? []);
        Assert.Equal("manual", hint.Type);
        Assert.Equal("navigation", hint.Target);
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

    private static string WriteSchemaV2Catalog(string root)
    {
        var templateRoot = Path.Combine(root, "screen", "main-menu");
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
                  "id": "screen/main-menu",
                  "displayName": "Main Menu Screen",
                  "description": "Reusable main menu screen.",
                  "version": "0.1.0",
                  "category": "screen",
                  "manifest": "screen/main-menu/fabricator-template.json",
                  "tags": ["screen", "menu"]
                }
              ]
            }
            """);

        File.WriteAllText(
            Path.Combine(templateRoot, "fabricator-template.json"),
            """
            {
              "schemaVersion": 2,
              "kind": "fabricator-template",
              "id": "screen/main-menu",
              "displayName": "Main Menu Screen",
              "description": "Reusable main menu screen.",
              "version": "0.1.0",
              "mode": "apply",
              "category": "screen",
              "tags": ["screen", "menu"],
              "files": [
                {
                  "path": "src/screens/MainMenuScreen.tsx",
                  "type": "file",
                  "targetFolder": "screens",
                  "targetPath": "src/screens/MainMenuScreen.tsx",
                  "description": "Main menu screen component."
                }
              ],
              "dependencies": [
                {
                  "type": "npm",
                  "name": "@react-navigation/native",
                  "version": "^7.0.0",
                  "reason": "Required when wired into navigation."
                }
              ],
              "exports": [
                {
                  "integrationPoint": "screensBarrel",
                  "statement": "export { MainMenuScreen } from './MainMenuScreen';",
                  "source": "src/screens/MainMenuScreen.tsx"
                }
              ],
              "integrationHints": [
                {
                  "type": "manual",
                  "target": "navigation",
                  "message": "Add MainMenuScreen to navigation if needed."
                }
              ]
            }
            """);

        File.WriteAllText(
            Path.Combine(templateRoot, "src", "screens", "MainMenuScreen.tsx"),
            "export function MainMenuScreen() { return null; }\n");

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
