using Fabricator.Core.Templates;

namespace Fabricator.Tests.Templates;

public sealed class TemplateCatalogValidationServiceTests
{
    [Fact]
    public async Task ValidateAsyncReturnsSuccessForValidCatalog()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path);
        var service = new TemplateCatalogValidationService();

        var result = await service.ValidateAsync(new TemplateCatalogValidationRequest(catalogPath));

        Assert.True(result.Succeeded);
        Assert.Equal(catalogPath, result.Source);
        Assert.Equal(1, result.TemplateCount);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateAsyncReportsDuplicateTemplateIds()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path, duplicateEntry: true);
        var service = new TemplateCatalogValidationService();

        var result = await service.ValidateAsync(new TemplateCatalogValidationRequest(catalogPath));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "duplicate-template-id" &&
                     issue.TemplateId == "minimal-splash");
    }

    [Fact]
    public async Task ValidateAsyncReportsMissingManifest()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path, writeManifest: false);
        var service = new TemplateCatalogValidationService();

        var result = await service.ValidateAsync(new TemplateCatalogValidationRequest(catalogPath));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "template-read-failed" &&
                     issue.Message.Contains("Template manifest was not found", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsyncReportsMissingTemplateFile()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path, writeTemplateFile: false);
        var service = new TemplateCatalogValidationService();

        var result = await service.ValidateAsync(new TemplateCatalogValidationRequest(catalogPath));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "template-read-failed" &&
                     issue.Message.Contains("Template file was not found", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsyncReportsUnsupportedTemplateSchema()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(directory.Path, templateSchemaVersion: 99);
        var service = new TemplateCatalogValidationService();

        var result = await service.ValidateAsync(new TemplateCatalogValidationRequest(catalogPath));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "unsupported-template-schema" &&
                     issue.TemplateId == "minimal-splash");
    }

    [Fact]
    public async Task ValidateAsyncReportsUnsupportedCategoryAndUnsafeExport()
    {
        using var directory = new TemporaryDirectory();
        var catalogPath = WriteCatalog(
            directory.Path,
            category: "unknown",
            exportStatement: "console.log('not an export');");
        var service = new TemplateCatalogValidationService();

        var result = await service.ValidateAsync(new TemplateCatalogValidationRequest(catalogPath));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported-template-category");
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported-export-statement");
    }

    private static string WriteCatalog(
        string root,
        bool duplicateEntry = false,
        bool writeManifest = true,
        bool writeTemplateFile = true,
        int templateSchemaVersion = 2,
        string category = "starter",
        string exportStatement = "export { App } from './App';")
    {
        var templateRoot = Path.Combine(root, "minimal-splash");
        Directory.CreateDirectory(templateRoot);

        var duplicateEntryJson = duplicateEntry
            ? """
                ,
                {
                  "id": "minimal-splash",
                  "displayName": "Minimal Splash Duplicate",
                  "description": "Duplicate starter.",
                  "version": "0.1.0",
                  "category": "starter",
                  "manifest": "minimal-splash/fabricator-template.json",
                  "tags": ["starter"]
                }
              """
            : string.Empty;

        File.WriteAllText(
            Path.Combine(root, "catalog.fabricator.json"),
            $$"""
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
                  "category": "{{category}}",
                  "manifest": "minimal-splash/fabricator-template.json",
                  "tags": ["starter"]
                }{{duplicateEntryJson}}
              ]
            }
            """);

        if (writeManifest)
        {
            File.WriteAllText(
                Path.Combine(templateRoot, "fabricator-template.json"),
                $$"""
                {
                  "schemaVersion": {{templateSchemaVersion}},
                  "kind": "fabricator-template",
                  "id": "minimal-splash",
                  "displayName": "Minimal Splash",
                  "description": "Minimal starter.",
                  "version": "0.1.0",
                  "mode": "starter",
                  "category": "{{category}}",
                  "files": [
                    {
                      "path": "App.tsx",
                      "type": "file",
                      "targetPath": "App.tsx"
                    }
                  ],
                  "exports": [
                    {
                      "integrationPoint": "appBarrel",
                      "statement": "{{exportStatement}}"
                    }
                  ]
                }
                """);
        }

        if (writeTemplateFile)
        {
            File.WriteAllText(Path.Combine(templateRoot, "App.tsx"), "export function App() { return null; }\n");
        }

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
