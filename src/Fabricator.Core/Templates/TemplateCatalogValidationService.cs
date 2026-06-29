using System.Text.Json;

namespace Fabricator.Core.Templates;

public sealed class TemplateCatalogValidationService : ITemplateCatalogValidationService
{
    private static readonly HashSet<int> SupportedCatalogSchemaVersions = [1];
    private static readonly HashSet<int> SupportedTemplateSchemaVersions = [1, 2];
    private static readonly HashSet<string> SupportedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "starter",
        "screen",
        "screens",
        "component",
        "components",
        "service",
        "services",
        "util",
        "utils",
        "integration",
        "integrations",
        "layout",
        "layouts",
        "navigation",
        "auth",
        "config"
    };

    private readonly ITemplateCatalogProvider _templateCatalogProvider;

    public TemplateCatalogValidationService()
        : this(new TemplateCatalogProvider())
    {
    }

    public TemplateCatalogValidationService(ITemplateCatalogProvider templateCatalogProvider)
    {
        _templateCatalogProvider = templateCatalogProvider;
    }

    public async Task<TemplateCatalogValidationResult> ValidateAsync(
        TemplateCatalogValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Source);

        var issues = new List<TemplateCatalogValidationIssue>();
        FabricatorTemplateCatalog? catalog = null;

        try
        {
            catalog = await _templateCatalogProvider.ListTemplatesAsync(request.Source, cancellationToken);
        }
        catch (Exception exception) when (exception is TemplatePackageException or JsonException)
        {
            issues.Add(new TemplateCatalogValidationIssue("catalog-read-failed", exception.Message));

            return new TemplateCatalogValidationResult(request.Source, 0, issues);
        }

        ValidateCatalog(catalog, issues);
        ValidateDuplicateTemplateIds(catalog, issues);

        foreach (var entry in catalog.Templates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ValidateTemplateAsync(request.Source, entry, issues, cancellationToken);
        }

        return new TemplateCatalogValidationResult(request.Source, catalog.Templates.Count, issues);
    }

    private static void ValidateCatalog(
        FabricatorTemplateCatalog catalog,
        List<TemplateCatalogValidationIssue> issues)
    {
        if (!SupportedCatalogSchemaVersions.Contains(catalog.SchemaVersion))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "unsupported-catalog-schema",
                $"Catalog schema version '{catalog.SchemaVersion}' is not supported."));
        }

        if (!string.Equals(catalog.Kind, "fabricator-template-catalog", StringComparison.Ordinal))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "invalid-catalog-kind",
                $"Catalog kind must be 'fabricator-template-catalog' but was '{catalog.Kind}'."));
        }

        if (catalog.Templates.Count == 0)
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "empty-catalog",
                "Catalog does not contain any templates."));
        }
    }

    private static void ValidateDuplicateTemplateIds(
        FabricatorTemplateCatalog catalog,
        List<TemplateCatalogValidationIssue> issues)
    {
        var duplicateIds = catalog.Templates
            .GroupBy(template => template.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "duplicate-template-id",
                $"Template id appears more than once in the catalog: {duplicateId}",
                duplicateId));
        }
    }

    private async Task ValidateTemplateAsync(
        string source,
        FabricatorTemplateCatalogEntry entry,
        List<TemplateCatalogValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        ValidateCatalogEntry(entry, issues);

        FabricatorTemplatePackage package;
        try
        {
            package = await _templateCatalogProvider.GetTemplateAsync(source, entry.Id, cancellationToken);
        }
        catch (Exception exception) when (exception is TemplatePackageException or InvalidOperationException)
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "template-read-failed",
                exception.Message,
                entry.Id));

            return;
        }

        ValidateManifest(entry, package.Manifest, issues);
        ValidateTemplateFiles(package, issues);
        ValidateTemplateExports(package.Manifest, issues);
    }

    private static void ValidateCatalogEntry(
        FabricatorTemplateCatalogEntry entry,
        List<TemplateCatalogValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "missing-template-id",
                "Catalog entry is missing a template id."));
        }

        if (string.IsNullOrWhiteSpace(entry.Manifest))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "missing-template-manifest",
                $"Catalog entry '{entry.Id}' is missing a manifest path.",
                entry.Id));
        }

        ValidateCategory(entry.Category, entry.Id, "catalog", issues);
    }

    private static void ValidateManifest(
        FabricatorTemplateCatalogEntry entry,
        FabricatorTemplateManifest manifest,
        List<TemplateCatalogValidationIssue> issues)
    {
        if (!SupportedTemplateSchemaVersions.Contains(manifest.SchemaVersion))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "unsupported-template-schema",
                $"Template schema version '{manifest.SchemaVersion}' is not supported.",
                manifest.Id));
        }

        if (!string.Equals(manifest.Kind, "fabricator-template", StringComparison.Ordinal))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "invalid-template-kind",
                $"Template kind must be 'fabricator-template' but was '{manifest.Kind}'.",
                manifest.Id));
        }

        if (!string.Equals(entry.Id, manifest.Id, StringComparison.Ordinal))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "template-id-mismatch",
                $"Catalog entry id '{entry.Id}' does not match manifest id '{manifest.Id}'.",
                entry.Id));
        }

        if (string.IsNullOrWhiteSpace(manifest.Mode))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "missing-template-mode",
                "Template mode is required.",
                manifest.Id));
        }

        if (manifest.Files.Count == 0)
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "template-without-files",
                "Template manifest does not contain any files.",
                manifest.Id));
        }

        ValidateCategory(manifest.Category, manifest.Id, "manifest", issues);
    }

    private static void ValidateTemplateFiles(
        FabricatorTemplatePackage package,
        List<TemplateCatalogValidationIssue> issues)
    {
        foreach (var file in package.Manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(file.Path))
            {
                issues.Add(new TemplateCatalogValidationIssue(
                    "missing-template-file-path",
                    "Template file path is required.",
                    package.Manifest.Id));
                continue;
            }

            if (Path.IsPathRooted(file.Path) || file.Path.Contains("..", StringComparison.Ordinal))
            {
                issues.Add(new TemplateCatalogValidationIssue(
                    "unsafe-template-file-path",
                    $"Template file path must stay inside the template folder: {file.Path}",
                    package.Manifest.Id));
            }

            if (!string.IsNullOrWhiteSpace(file.TargetPath) &&
                (Path.IsPathRooted(file.TargetPath) || file.TargetPath.Contains("..", StringComparison.Ordinal)))
            {
                issues.Add(new TemplateCatalogValidationIssue(
                    "unsafe-template-target-path",
                    $"Template target path must stay inside the project root: {file.TargetPath}",
                    package.Manifest.Id));
            }
        }
    }

    private static void ValidateTemplateExports(
        FabricatorTemplateManifest manifest,
        List<TemplateCatalogValidationIssue> issues)
    {
        foreach (var templateExport in manifest.Exports ?? [])
        {
            var statement = templateExport.Statement?.Trim();
            if (string.IsNullOrWhiteSpace(templateExport.IntegrationPoint))
            {
                issues.Add(new TemplateCatalogValidationIssue(
                    "missing-export-integration-point",
                    "Template export integration point is required.",
                    manifest.Id));
            }

            if (!IsSafeBarrelExportStatement(statement))
            {
                issues.Add(new TemplateCatalogValidationIssue(
                    "unsupported-export-statement",
                    $"Export statement is not a supported single-line barrel export: {templateExport.Statement}",
                    manifest.Id));
            }
        }
    }

    private static void ValidateCategory(
        string? category,
        string? templateId,
        string source,
        List<TemplateCatalogValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        if (!SupportedCategories.Contains(category))
        {
            issues.Add(new TemplateCatalogValidationIssue(
                "unsupported-template-category",
                $"Template {source} category '{category}' is not supported.",
                templateId));
        }
    }

    private static bool IsSafeBarrelExportStatement(string? statement)
    {
        if (string.IsNullOrWhiteSpace(statement))
        {
            return false;
        }

        return statement.StartsWith("export ", StringComparison.Ordinal)
               && statement.Contains(" from ", StringComparison.Ordinal)
               && statement.EndsWith(';')
               && statement.Count(character => character == ';') == 1
               && !statement.Contains('`')
               && !statement.Contains('\n')
               && !statement.Contains('\r');
    }
}
