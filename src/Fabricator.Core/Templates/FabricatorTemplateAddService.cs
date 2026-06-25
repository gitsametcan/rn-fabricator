using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateAddService
{
    private const string CatalogKind = "fabricator-template-catalog";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly FabricatorTemplateCaptureService _captureService;

    public FabricatorTemplateAddService()
        : this(new FabricatorTemplateCaptureService())
    {
    }

    public FabricatorTemplateAddService(FabricatorTemplateCaptureService captureService)
    {
        _captureService = captureService;
    }

    public async Task<FabricatorTemplateAddResult> AddAsync(
        FabricatorTemplateAddRequest request,
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
            return Failed(request, catalogPath, string.Empty, string.Empty, errors);
        }

        var catalogRead = ReadOrCreateCatalog(catalogPath);
        if (catalogRead.Errors.Count > 0 || catalogRead.Catalog is null || catalogRead.Templates is null)
        {
            return Failed(request, catalogPath, string.Empty, string.Empty, catalogRead.Errors);
        }

        if (ContainsTemplateId(catalogRead.Templates, request.TemplateId))
        {
            return Failed(
                request,
                catalogPath,
                string.Empty,
                string.Empty,
                [$"Template id already exists in catalog: {request.TemplateId}. Use the update flow when changing an existing template."]);
        }

        var captureResult = await _captureService.CaptureAsync(
            new FabricatorTemplateCaptureRequest(
                request.TemplateId,
                request.Category,
                request.SourceProjectDirectory,
                catalogDirectory),
            cancellationToken);

        if (!captureResult.Succeeded)
        {
            return Failed(
                request,
                catalogPath,
                captureResult.TemplateDirectory,
                captureResult.ManifestPath,
                captureResult.Errors);
        }

        try
        {
            var manifest = await ReadManifestAsync(captureResult.ManifestPath, cancellationToken);
            catalogRead.Templates.Add(CreateCatalogEntry(manifest, catalogDirectory, captureResult.ManifestPath));
            catalogRead.Catalog["templates"] = SortTemplates(catalogRead.Templates);

            await WriteCatalogAsync(catalogPath, catalogRead.Catalog, cancellationToken);

            return new FabricatorTemplateAddResult(
                request.TemplateId,
                catalogPath,
                captureResult.TemplateDirectory,
                captureResult.ManifestPath,
                captureResult.CapturedFiles,
                []);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            DeleteCapturedTemplate(captureResult.TemplateDirectory);

            return Failed(
                request,
                catalogPath,
                captureResult.TemplateDirectory,
                captureResult.ManifestPath,
                [$"Template catalog could not be updated: {exception.Message}"]);
        }
    }

    private static List<string> ValidateRequest(FabricatorTemplateAddRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            errors.Add("Template id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            errors.Add("Template category is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceProjectDirectory))
        {
            errors.Add("Source project directory is required.");
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
            errors.Add("Template add requires a local catalog path. Remote catalog URLs are read-only.");
            return string.Empty;
        }

        var expandedSource = catalogSource.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                catalogSource[2..])
            : catalogSource;

        return Path.GetFullPath(expandedSource);
    }

    private static CatalogReadResult ReadOrCreateCatalog(string catalogPath)
    {
        if (!File.Exists(catalogPath))
        {
            var catalog = new JsonObject
            {
                ["schemaVersion"] = 1,
                ["kind"] = CatalogKind,
                ["displayName"] = "Local Fabricator template catalog",
                ["description"] = "Local template catalog managed by rn-fabricator.",
                ["templates"] = new JsonArray()
            };

            return CatalogReadResult.Success(catalog, catalog["templates"]!.AsArray());
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

    private static bool ContainsTemplateId(JsonArray templates, string templateId)
    {
        return templates
            .OfType<JsonObject>()
            .Any(template => string.Equals(GetString(template, "id"), templateId, StringComparison.Ordinal));
    }

    private static async Task<FabricatorTemplateManifest> ReadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        var manifest = JsonSerializer.Deserialize<FabricatorTemplateManifest>(
            await File.ReadAllTextAsync(manifestPath, cancellationToken),
            JsonOptions);

        return manifest ?? throw new JsonException($"Template manifest could not be read: {manifestPath}");
    }

    private static JsonObject CreateCatalogEntry(
        FabricatorTemplateManifest manifest,
        string catalogDirectory,
        string manifestPath)
    {
        return new JsonObject
        {
            ["id"] = manifest.Id,
            ["displayName"] = manifest.DisplayName,
            ["description"] = manifest.Description,
            ["version"] = manifest.Version,
            ["category"] = manifest.Category ?? string.Empty,
            ["manifest"] = Path.GetRelativePath(catalogDirectory, manifestPath).Replace('\\', '/'),
            ["tags"] = CreateStringArray(manifest.Tags ?? [])
        };
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

    private static JsonArray CreateStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();

        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
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

    private static FabricatorTemplateAddResult Failed(
        FabricatorTemplateAddRequest request,
        string catalogPath,
        string templateDirectory,
        string manifestPath,
        IReadOnlyList<string> errors)
    {
        return new FabricatorTemplateAddResult(
            request.TemplateId,
            catalogPath,
            templateDirectory,
            manifestPath,
            [],
            errors);
    }

    private static void DeleteCapturedTemplate(string templateDirectory)
    {
        if (Directory.Exists(templateDirectory))
        {
            Directory.Delete(templateDirectory, recursive: true);
        }
    }

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
