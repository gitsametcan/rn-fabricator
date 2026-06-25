using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateRemoveService
{
    private const string CatalogKind = "fabricator-template-catalog";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public async Task<FabricatorTemplateRemoveResult> RemoveAsync(
        FabricatorTemplateRemoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = ValidateRequest(request);
        var catalogPath = ResolveCatalogPath(request.CatalogSource, errors);
        var catalogDirectory = string.IsNullOrWhiteSpace(catalogPath)
            ? string.Empty
            : Path.GetDirectoryName(catalogPath) ?? Directory.GetCurrentDirectory();

        if (errors.Count > 0)
        {
            return Failed(request, catalogPath, null, errors);
        }

        var catalogRead = ReadCatalog(catalogPath);
        if (catalogRead.Errors.Count > 0 || catalogRead.Catalog is null || catalogRead.Templates is null)
        {
            return Failed(request, catalogPath, null, catalogRead.Errors);
        }

        var template = FindTemplate(catalogRead.Templates, request.TemplateId);
        if (template is null)
        {
            return Failed(request, catalogPath, null, [$"Template id was not found in catalog: {request.TemplateId}"]);
        }

        var templateDirectory = request.DeleteFiles
            ? ResolveTemplateDirectory(catalogDirectory, template, errors)
            : TryResolveTemplateDirectory(catalogDirectory, template);
        if (request.DeleteFiles && errors.Count > 0)
        {
            return Failed(request, catalogPath, templateDirectory, errors);
        }

        try
        {
            RemoveTemplate(catalogRead.Templates, request.TemplateId);
            catalogRead.Catalog["templates"] = SortTemplates(catalogRead.Templates);
            await WriteCatalogAsync(catalogPath, catalogRead.Catalog, cancellationToken);

            var deletedFiles = false;
            if (request.DeleteFiles)
            {
                if (string.IsNullOrWhiteSpace(templateDirectory))
                {
                    return Failed(request, catalogPath, templateDirectory, ["Template directory could not be resolved for deletion."]);
                }

                if (Directory.Exists(templateDirectory))
                {
                    Directory.Delete(templateDirectory, recursive: true);
                    deletedFiles = true;
                }
            }

            return new FabricatorTemplateRemoveResult(
                request.TemplateId,
                catalogPath,
                templateDirectory,
                catalogEntryRemoved: true,
                templateFilesDeleted: deletedFiles,
                []);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return Failed(request, catalogPath, templateDirectory, [$"Template remove failed: {exception.Message}"]);
        }
    }

    private static List<string> ValidateRequest(FabricatorTemplateRemoveRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            errors.Add("Template id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CatalogSource))
        {
            errors.Add("Local catalog source is required.");
        }

        return errors;
    }

    private static string ResolveCatalogPath(string catalogSource, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(catalogSource))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(catalogSource, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Template remove requires a local catalog path. Remote catalog URLs are read-only.");
            return string.Empty;
        }

        var expandedSource = catalogSource.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                catalogSource[2..])
            : catalogSource;

        return Path.GetFullPath(expandedSource);
    }

    private static CatalogReadResult ReadCatalog(string catalogPath)
    {
        if (!File.Exists(catalogPath))
        {
            return CatalogReadResult.Failed([$"Template catalog was not found: {catalogPath}"]);
        }

        try
        {
            var catalog = JsonNode.Parse(File.ReadAllText(catalogPath)) as JsonObject;
            if (catalog is null)
            {
                return CatalogReadResult.Failed([$"Template catalog could not be read as an object: {catalogPath}"]);
            }

            var errors = ValidateCatalog(catalog, catalogPath);
            if (errors.Count > 0)
            {
                return CatalogReadResult.Failed(errors);
            }

            return CatalogReadResult.Success(catalog, catalog["templates"]!.AsArray());
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return CatalogReadResult.Failed([$"Template catalog could not be read: {catalogPath}. {exception.Message}"]);
        }
    }

    private static IReadOnlyList<string> ValidateCatalog(JsonObject catalog, string catalogPath)
    {
        var errors = new List<string>();

        if (catalog["schemaVersion"]?.GetValue<int>() != 1)
        {
            errors.Add($"Template catalog schema is not supported: {catalogPath}");
        }

        if (!string.Equals(catalog["kind"]?.GetValue<string>(), CatalogKind, StringComparison.Ordinal))
        {
            errors.Add($"Template catalog kind is invalid: {catalogPath}");
        }

        if (catalog["templates"] is not JsonArray templates)
        {
            errors.Add($"Template catalog must contain a templates array: {catalogPath}");
            return errors;
        }

        foreach (var template in templates)
        {
            if (template is not JsonObject templateObject || string.IsNullOrWhiteSpace(GetString(templateObject, "id")))
            {
                errors.Add($"Template catalog entries must be objects with id fields: {catalogPath}");
                break;
            }
        }

        return errors;
    }

    private static JsonObject? FindTemplate(JsonArray templates, string templateId)
    {
        return templates
            .OfType<JsonObject>()
            .SingleOrDefault(template => string.Equals(GetString(template, "id"), templateId, StringComparison.Ordinal));
    }

    private static string? ResolveTemplateDirectory(
        string catalogDirectory,
        JsonObject template,
        List<string> errors)
    {
        var manifest = GetString(template, "manifest");
        if (string.IsNullOrWhiteSpace(manifest))
        {
            errors.Add("Template catalog entry must include a manifest path.");
            return null;
        }

        if (Path.IsPathRooted(manifest))
        {
            errors.Add("Template manifest path must be relative to the catalog directory.");
            return null;
        }

        var manifestPath = Path.GetFullPath(Path.Combine(catalogDirectory, manifest));
        if (!IsChildPath(catalogDirectory, manifestPath))
        {
            errors.Add($"Template manifest resolved outside the catalog directory: {manifest}");
            return null;
        }

        var templateDirectory = Path.GetDirectoryName(manifestPath);
        if (string.IsNullOrWhiteSpace(templateDirectory))
        {
            errors.Add("Template directory could not be resolved from manifest path.");
            return null;
        }

        if (string.Equals(Path.GetFullPath(catalogDirectory), Path.GetFullPath(templateDirectory), PathComparison))
        {
            errors.Add("Refusing to delete the catalog directory. Template manifest must live in its own template folder.");
            return null;
        }

        if (!IsChildPath(catalogDirectory, templateDirectory))
        {
            errors.Add("Template directory resolved outside the catalog directory.");
            return null;
        }

        return templateDirectory;
    }

    private static string? TryResolveTemplateDirectory(string catalogDirectory, JsonObject template)
    {
        var manifest = GetString(template, "manifest");
        if (string.IsNullOrWhiteSpace(manifest) || Path.IsPathRooted(manifest))
        {
            return null;
        }

        var manifestPath = Path.GetFullPath(Path.Combine(catalogDirectory, manifest));
        if (!IsChildPath(catalogDirectory, manifestPath))
        {
            return null;
        }

        var templateDirectory = Path.GetDirectoryName(manifestPath);
        if (string.IsNullOrWhiteSpace(templateDirectory) ||
            string.Equals(Path.GetFullPath(catalogDirectory), Path.GetFullPath(templateDirectory), PathComparison) ||
            !IsChildPath(catalogDirectory, templateDirectory))
        {
            return null;
        }

        return templateDirectory;
    }

    private static void RemoveTemplate(JsonArray templates, string templateId)
    {
        for (var index = templates.Count - 1; index >= 0; index--)
        {
            if (templates[index] is JsonObject template &&
                string.Equals(GetString(template, "id"), templateId, StringComparison.Ordinal))
            {
                templates.RemoveAt(index);
            }
        }
    }

    private static JsonArray SortTemplates(JsonArray templates)
    {
        var sorted = new JsonArray();

        foreach (var template in templates
                     .OfType<JsonObject>()
                     .OrderBy(template => GetString(template, "id"), StringComparer.Ordinal))
        {
            sorted.Add(template.DeepClone());
        }

        return sorted;
    }

    private static async Task WriteCatalogAsync(
        string catalogPath,
        JsonObject catalog,
        CancellationToken cancellationToken)
    {
        var catalogDirectory = Path.GetDirectoryName(catalogPath);
        if (!string.IsNullOrWhiteSpace(catalogDirectory))
        {
            Directory.CreateDirectory(catalogDirectory);
        }

        var temporaryPath = Path.Combine(
            catalogDirectory ?? Directory.GetCurrentDirectory(),
            $".{Path.GetFileName(catalogPath)}.tmp-{Guid.NewGuid():N}");

        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                catalog.ToJsonString(JsonOptions) + System.Environment.NewLine,
                cancellationToken);
            File.Move(temporaryPath, catalogPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string GetString(JsonObject jsonObject, string propertyName)
    {
        return jsonObject[propertyName]?.GetValue<string>() ?? string.Empty;
    }

    private static FabricatorTemplateRemoveResult Failed(
        FabricatorTemplateRemoveRequest request,
        string catalogPath,
        string? templateDirectory,
        IReadOnlyList<string> errors)
    {
        return new FabricatorTemplateRemoveResult(
            request.TemplateId,
            catalogPath,
            templateDirectory,
            catalogEntryRemoved: false,
            templateFilesDeleted: false,
            errors);
    }

    private static bool IsChildPath(string parentPath, string childPath)
    {
        var parent = EnsureTrailingSeparator(Path.GetFullPath(parentPath));
        var child = Path.GetFullPath(childPath);

        return child.StartsWith(parent, PathComparison);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private sealed record CatalogReadResult(
        JsonObject? Catalog,
        JsonArray? Templates,
        IReadOnlyList<string> Errors)
    {
        public static CatalogReadResult Success(JsonObject catalog, JsonArray templates)
        {
            return new CatalogReadResult(catalog, templates, []);
        }

        public static CatalogReadResult Failed(IReadOnlyList<string> errors)
        {
            return new CatalogReadResult(null, null, errors);
        }
    }
}
