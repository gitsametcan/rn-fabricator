using Fabricator.Core.Projects;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateUpdateService
{
    private const string CatalogKind = "fabricator-template-catalog";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly IFabricatorProjectCompatibilityValidator _compatibilityValidator;

    public FabricatorTemplateUpdateService()
        : this(new FabricatorProjectCompatibilityValidator())
    {
    }

    public FabricatorTemplateUpdateService(IFabricatorProjectCompatibilityValidator compatibilityValidator)
    {
        _compatibilityValidator = compatibilityValidator;
    }

    public async Task<FabricatorTemplateUpdateResult> UpdateAsync(
        FabricatorTemplateUpdateRequest request,
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

        var catalogRead = ReadCatalog(catalogPath);
        if (catalogRead.Errors.Count > 0 || catalogRead.Catalog is null || catalogRead.Templates is null)
        {
            return Failed(request, catalogPath, string.Empty, string.Empty, catalogRead.Errors);
        }

        var catalogEntry = FindTemplate(catalogRead.Templates, request.TemplateId);
        if (catalogEntry is null)
        {
            return Failed(request, catalogPath, string.Empty, string.Empty, [$"Template id was not found in catalog: {request.TemplateId}"]);
        }

        var manifestPath = ResolveManifestPath(catalogDirectory, catalogEntry, errors);
        if (errors.Count > 0)
        {
            return Failed(request, catalogPath, string.Empty, manifestPath, errors);
        }

        var templateDirectory = Path.GetDirectoryName(manifestPath) ?? catalogDirectory;

        try
        {
            var existingManifest = await ReadManifestAsync(manifestPath, cancellationToken);
            if (!string.Equals(existingManifest.Id, request.TemplateId, StringComparison.Ordinal))
            {
                return Failed(
                    request,
                    catalogPath,
                    templateDirectory,
                    manifestPath,
                    [$"Template manifest id '{existingManifest.Id}' does not match catalog template '{request.TemplateId}'."]);
            }

            var categoryValue = ResolveCategory(existingManifest, catalogEntry);
            if (string.IsNullOrWhiteSpace(categoryValue))
            {
                return Failed(
                    request,
                    catalogPath,
                    templateDirectory,
                    manifestPath,
                    [$"Template category is required in the manifest or catalog entry: {request.TemplateId}"]);
            }

            var category = FabricatorTemplateCategoryNormalizer.Normalize(categoryValue);
            var capture = CaptureProjectFiles(request, category.FolderKey, cancellationToken);
            if (capture.Errors.Count > 0)
            {
                return Failed(request, catalogPath, templateDirectory, manifestPath, capture.Errors);
            }

            var updatedManifest = PreserveMetadata(existingManifest, category.CanonicalCategory, capture.TemplateFiles);
            var projectRoot = Path.GetFullPath(request.SourceProjectDirectory);
            var summary = CreateSummary(
                projectRoot,
                templateDirectory,
                existingManifest.Files,
                capture.TemplateFiles);

            if (!request.DryRun)
            {
                await ReplaceTemplateDirectoryAsync(
                    projectRoot,
                    templateDirectory,
                    updatedManifest,
                    capture.TemplateFiles,
                    cancellationToken);

                UpdateCatalogEntry(catalogEntry, updatedManifest, catalogDirectory, manifestPath);
                catalogRead.Catalog["templates"] = SortTemplates(catalogRead.Templates);
                await WriteCatalogAsync(catalogPath, catalogRead.Catalog, cancellationToken);
            }

            return new FabricatorTemplateUpdateResult(
                request.TemplateId,
                catalogPath,
                templateDirectory,
                manifestPath,
                summary.AddedFiles,
                summary.ChangedFiles,
                summary.UnchangedFiles,
                summary.RemovedFiles,
                [
                    "displayName",
                    "description",
                    "version",
                    "mode",
                    "tags",
                    "dependencies",
                    "exports",
                    "integrationHints"
                ],
                [],
                request.DryRun);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return Failed(
                request,
                catalogPath,
                templateDirectory,
                manifestPath,
                [$"Template update failed: {exception.Message}"]);
        }
    }

    private static List<string> ValidateRequest(FabricatorTemplateUpdateRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            errors.Add("Template id is required.");
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
            errors.Add("Template update requires a local catalog path. Remote catalog URLs are read-only.");
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

    private static string ResolveManifestPath(
        string catalogDirectory,
        JsonObject catalogEntry,
        List<string> errors)
    {
        var manifest = GetString(catalogEntry, "manifest");
        if (string.IsNullOrWhiteSpace(manifest))
        {
            errors.Add("Template catalog entry must include a manifest path.");
            return string.Empty;
        }

        if (Path.IsPathRooted(manifest))
        {
            errors.Add("Template manifest path must be relative to the catalog directory.");
            return string.Empty;
        }

        var manifestPath = Path.GetFullPath(Path.Combine(catalogDirectory, manifest));
        if (!IsChildPath(catalogDirectory, manifestPath))
        {
            errors.Add($"Template manifest resolved outside the catalog directory: {manifest}");
        }

        return manifestPath;
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

    private static string ResolveCategory(FabricatorTemplateManifest manifest, JsonObject catalogEntry)
    {
        return !string.IsNullOrWhiteSpace(manifest.Category)
            ? manifest.Category
            : GetString(catalogEntry, "category");
    }

    private ProjectCaptureResult CaptureProjectFiles(
        FabricatorTemplateUpdateRequest request,
        string category,
        CancellationToken cancellationToken)
    {
        var projectRoot = Path.GetFullPath(request.SourceProjectDirectory);
        var compatibility = _compatibilityValidator.Validate(projectRoot);
        if (!compatibility.IsCompatible)
        {
            return ProjectCaptureResult.Failed(compatibility.Errors);
        }

        var projectManifest = compatibility.Manifest
            ?? throw new InvalidOperationException("Compatible Fabricator projects must include a manifest.");
        var selection = FabricatorProjectTemplateFileSelector.Select(
            projectManifest,
            projectRoot,
            category,
            request.IncludePaths,
            cancellationToken);

        return selection.Succeeded
            ? ProjectCaptureResult.Success(selection.Files)
            : ProjectCaptureResult.Failed(selection.Errors);
    }

    private static FabricatorTemplateManifest PreserveMetadata(
        FabricatorTemplateManifest existingManifest,
        string category,
        IReadOnlyList<FabricatorTemplateFile> updatedFiles)
    {
        return existingManifest with
        {
            Category = category,
            Files = updatedFiles
        };
    }

    private static FileChangeSummary CreateSummary(
        string projectDirectory,
        string templateDirectory,
        IReadOnlyList<FabricatorTemplateFile> existingFiles,
        IReadOnlyList<FabricatorTemplateFile> updatedFiles)
    {
        var existingPaths = existingFiles
            .Select(file => file.Path)
            .ToHashSet(StringComparer.Ordinal);
        var updatedPaths = updatedFiles
            .Select(file => file.Path)
            .ToHashSet(StringComparer.Ordinal);
        var addedFiles = updatedPaths.Except(existingPaths, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var removedFiles = existingPaths.Except(updatedPaths, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var changedFiles = new List<string>();
        var unchangedFiles = new List<string>();

        foreach (var path in updatedPaths.Intersect(existingPaths, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var sourcePath = Path.Combine(projectDirectory, path);
            var existingTemplatePath = Path.Combine(templateDirectory, path);

            if (!File.Exists(existingTemplatePath) ||
                !string.Equals(File.ReadAllText(sourcePath), File.ReadAllText(existingTemplatePath), StringComparison.Ordinal))
            {
                changedFiles.Add(path);
            }
            else
            {
                unchangedFiles.Add(path);
            }
        }

        return new FileChangeSummary(
            addedFiles,
            changedFiles,
            unchangedFiles,
            removedFiles);
    }

    private static async Task ReplaceTemplateDirectoryAsync(
        string projectDirectory,
        string templateDirectory,
        FabricatorTemplateManifest updatedManifest,
        IReadOnlyList<FabricatorTemplateFile> updatedFiles,
        CancellationToken cancellationToken)
    {
        var templateParentDirectory = Path.GetDirectoryName(templateDirectory) ?? Directory.GetCurrentDirectory();
        var stagingDirectory = Path.Combine(templateParentDirectory, $".{Path.GetFileName(templateDirectory)}.update-tmp-{Guid.NewGuid():N}");
        var backupDirectory = Path.Combine(templateParentDirectory, $".{Path.GetFileName(templateDirectory)}.backup-{Guid.NewGuid():N}");
        var movedToBackup = false;

        try
        {
            Directory.CreateDirectory(stagingDirectory);

            foreach (var file in updatedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourcePath = Path.Combine(projectDirectory, file.Path);
                var targetPath = Path.GetFullPath(Path.Combine(stagingDirectory, file.Path));
                if (!IsChildPath(stagingDirectory, targetPath))
                {
                    throw new InvalidOperationException($"Template file resolved outside the staging directory: {file.Path}");
                }

                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath);
            }

            await File.WriteAllTextAsync(
                Path.Combine(stagingDirectory, "fabricator-template.json"),
                JsonSerializer.Serialize(updatedManifest, JsonOptions) + System.Environment.NewLine,
                cancellationToken);

            if (Directory.Exists(templateDirectory))
            {
                Directory.Move(templateDirectory, backupDirectory);
                movedToBackup = true;
            }

            Directory.Move(stagingDirectory, templateDirectory);

            if (movedToBackup)
            {
                Directory.Delete(backupDirectory, recursive: true);
            }
        }
        catch
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }

            if (movedToBackup && !Directory.Exists(templateDirectory) && Directory.Exists(backupDirectory))
            {
                Directory.Move(backupDirectory, templateDirectory);
            }

            throw;
        }
    }

    private static void UpdateCatalogEntry(
        JsonObject catalogEntry,
        FabricatorTemplateManifest manifest,
        string catalogDirectory,
        string manifestPath)
    {
        catalogEntry["displayName"] = manifest.DisplayName;
        catalogEntry["description"] = manifest.Description;
        catalogEntry["version"] = manifest.Version;
        catalogEntry["category"] = manifest.Category ?? string.Empty;
        catalogEntry["manifest"] = Path.GetRelativePath(catalogDirectory, manifestPath).Replace('\\', '/');
        catalogEntry["tags"] = CreateStringArray(manifest.Tags ?? []);
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

    private static FabricatorTemplateUpdateResult Failed(
        FabricatorTemplateUpdateRequest request,
        string catalogPath,
        string templateDirectory,
        string manifestPath,
        IReadOnlyList<string> errors)
    {
        return new FabricatorTemplateUpdateResult(
            request.TemplateId,
            catalogPath,
            templateDirectory,
            manifestPath,
            [],
            [],
            [],
            [],
            [],
            errors,
            request.DryRun);
    }

    private static bool IsChildPath(string parentPath, string childPath)
    {
        var parent = EnsureTrailingSeparator(Path.GetFullPath(parentPath));
        var child = Path.GetFullPath(childPath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return child.StartsWith(parent, comparison);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
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

    private sealed record ProjectCaptureResult(
        IReadOnlyList<FabricatorTemplateFile> TemplateFiles,
        IReadOnlyList<string> Errors)
    {
        public static ProjectCaptureResult Success(IReadOnlyList<FabricatorTemplateFile> templateFiles)
        {
            return new ProjectCaptureResult(templateFiles, []);
        }

        public static ProjectCaptureResult Failed(IReadOnlyList<string> errors)
        {
            return new ProjectCaptureResult([], errors);
        }
    }

    private sealed record FileChangeSummary(
        IReadOnlyList<string> AddedFiles,
        IReadOnlyList<string> ChangedFiles,
        IReadOnlyList<string> UnchangedFiles,
        IReadOnlyList<string> RemovedFiles);
}
