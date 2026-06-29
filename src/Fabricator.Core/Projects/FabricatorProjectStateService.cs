using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Core.Projects;

public sealed class FabricatorProjectStateService : IFabricatorProjectStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly IFabricatorProjectCompatibilityValidator _compatibilityValidator;
    private readonly ITemplateCatalogProvider _templateCatalogProvider;

    public FabricatorProjectStateService()
        : this(new FabricatorProjectCompatibilityValidator())
    {
    }

    public FabricatorProjectStateService(IFabricatorProjectCompatibilityValidator compatibilityValidator)
        : this(compatibilityValidator, new TemplateCatalogProvider())
    {
    }

    public FabricatorProjectStateService(
        IFabricatorProjectCompatibilityValidator compatibilityValidator,
        ITemplateCatalogProvider templateCatalogProvider)
    {
        _compatibilityValidator = compatibilityValidator;
        _templateCatalogProvider = templateCatalogProvider;
    }

    public FabricatorProjectStateUpdateResult ValidateCanTrack(string projectDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        var projectRoot = Path.GetFullPath(projectDirectory);
        var compatibility = _compatibilityValidator.Validate(projectRoot);

        if (!compatibility.IsCompatible)
        {
            return FabricatorProjectStateUpdateResult.Failed(compatibility.Errors);
        }

        var statePath = Path.Combine(projectRoot, FabricatorProjectStateContract.StateRelativePath);
        var stateRead = TryReadState(statePath);

        return stateRead.Errors.Count == 0
            ? FabricatorProjectStateUpdateResult.Success()
            : FabricatorProjectStateUpdateResult.Failed(stateRead.Errors);
    }

    public async Task<FabricatorProjectTemplateStatusResult> GetTemplateStatusAsync(
        string projectDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        var projectRoot = Path.GetFullPath(projectDirectory);
        var compatibility = _compatibilityValidator.Validate(projectRoot);

        if (!compatibility.IsCompatible)
        {
            return FabricatorProjectTemplateStatusResult.Failed(projectRoot, compatibility.Errors);
        }

        var statePath = Path.Combine(projectRoot, FabricatorProjectStateContract.StateRelativePath);
        var stateRead = TryReadState(statePath);

        if (stateRead.Errors.Count != 0 || stateRead.State is null)
        {
            return FabricatorProjectTemplateStatusResult.Failed(projectRoot, stateRead.Errors);
        }

        FabricatorProjectState? state;
        try
        {
            state = stateRead.State.Deserialize<FabricatorProjectState>(JsonOptions);
        }
        catch (JsonException exception)
        {
            return FabricatorProjectTemplateStatusResult.Failed(
                projectRoot,
                [$"Fabricator project state file could not be parsed: {statePath}. {exception.Message}"]);
        }

        if (state is null)
        {
            return FabricatorProjectTemplateStatusResult.Failed(
                projectRoot,
                [$"Fabricator project state file could not be parsed: {statePath}"]);
        }

        var catalogCache = new Dictionary<string, FabricatorTemplateCatalog?>(StringComparer.Ordinal);
        var statuses = new List<FabricatorAppliedTemplateStatus>();

        foreach (var template in state.AppliedTemplates)
        {
            var catalogStatus = await GetCatalogStatusAsync(projectRoot, template, catalogCache, cancellationToken);
            statuses.Add(new FabricatorAppliedTemplateStatus(
                template.Id,
                template.Version,
                template.Category,
                template.Operation,
                template.Result,
                template.AppliedAt,
                template.Source,
                template.Files,
                template.Exports.Count,
                template.IntegrationNotes.Count,
                catalogStatus.Status,
                catalogStatus.Version,
                catalogStatus.Source));
        }

        return FabricatorProjectTemplateStatusResult.Success(projectRoot, statuses);
    }

    public async Task<FabricatorProjectStateUpdateResult> TrackApplyAsync(
        FabricatorTemplateApplyStateTrackingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.ApplyResult.Succeeded)
        {
            return FabricatorProjectStateUpdateResult.Success();
        }

        var projectRoot = Path.GetFullPath(request.ProjectDirectory);
        var compatibility = _compatibilityValidator.Validate(projectRoot);
        if (!compatibility.IsCompatible)
        {
            return FabricatorProjectStateUpdateResult.Failed(compatibility.Errors);
        }

        var projectManifest = compatibility.Manifest
            ?? throw new InvalidOperationException("Compatible Fabricator projects must include a manifest.");
        var statePath = Path.Combine(projectRoot, FabricatorProjectStateContract.StateRelativePath);
        var stateRead = TryReadState(statePath);

        if (stateRead.Errors.Count != 0 || stateRead.State is null || stateRead.AppliedTemplates is null)
        {
            return FabricatorProjectStateUpdateResult.Failed(stateRead.Errors);
        }

        stateRead.State["toolVersion"] = ProductInfo.Version;
        stateRead.AppliedTemplates.Add(CreateApplyOperation(
            request,
            projectManifest));

        try
        {
            await File.WriteAllTextAsync(
                statePath,
                stateRead.State.ToJsonString(JsonOptions) + System.Environment.NewLine,
                cancellationToken);

            return FabricatorProjectStateUpdateResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return FabricatorProjectStateUpdateResult.Failed(
                [$"Failed to update project state file {FabricatorProjectStateContract.StateRelativePath}: {exception.Message}"]);
        }
    }

    private static JsonObject CreateApplyOperation(
        FabricatorTemplateApplyStateTrackingRequest request,
        FabricatorProjectManifest projectManifest)
    {
        var manifest = request.Package.Manifest;
        var source = CreateSourceObject(request.TemplateSource);
        var overwritten = request.ApplyResult.OverwrittenFiles;
        var written = request.ApplyResult.GeneratedFiles
            .Except(overwritten, StringComparer.Ordinal)
            .ToArray();
        var integrationPoints = projectManifest.IntegrationPoints
            .ToDictionary(point => point.Key, StringComparer.Ordinal);

        return new JsonObject
        {
            ["id"] = manifest.Id,
            ["version"] = manifest.Version,
            ["category"] = string.IsNullOrWhiteSpace(manifest.Category) ? "uncategorized" : manifest.Category,
            ["source"] = source,
            ["appliedAt"] = DateTimeOffset.UtcNow,
            ["operation"] = "apply",
            ["result"] = FabricatorProjectStateContract.ProjectStateResultApplied,
            ["files"] = new JsonObject
            {
                ["written"] = CreateStringArray(written),
                ["skipped"] = CreateStringArray(request.ApplyResult.SkippedFiles),
                ["overwritten"] = CreateStringArray(overwritten)
            },
            ["exports"] = CreateExportArray(
                request.ApplyResult.AppliedExports,
                request.ApplyResult.SkippedExports,
                integrationPoints),
            ["integrationNotes"] = CreateIntegrationNoteArray(request.ApplyResult.IntegrationReports)
        };
    }

    private static JsonObject CreateSourceObject(string source)
    {
        var sourceType = IsRemoteSource(source)
            ? FabricatorProjectStateContract.SourceTypeRemote
            : FabricatorProjectStateContract.SourceTypeLocal;
        var sourceName = sourceType == FabricatorProjectStateContract.SourceTypeRemote
            ? "remote"
            : "local";

        return new JsonObject
        {
            ["name"] = sourceName,
            ["type"] = sourceType,
            ["value"] = source,
            ["isDefault"] = false
        };
    }

    private async Task<CatalogStatusResult> GetCatalogStatusAsync(
        string projectRoot,
        FabricatorAppliedTemplateState template,
        Dictionary<string, FabricatorTemplateCatalog?> catalogCache,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(template.Source.Type, FabricatorProjectStateContract.SourceTypeLocal, StringComparison.Ordinal))
        {
            return new CatalogStatusResult("not-checked", null, null);
        }

        if (string.IsNullOrWhiteSpace(template.Source.Value))
        {
            return new CatalogStatusResult("catalog-missing", null, null);
        }

        var catalogSource = ResolveLocalSource(projectRoot, template.Source.Value);
        if (!File.Exists(catalogSource))
        {
            return new CatalogStatusResult("catalog-missing", null, catalogSource);
        }

        if (!catalogCache.TryGetValue(catalogSource, out var catalog))
        {
            try
            {
                catalog = await _templateCatalogProvider.ListTemplatesAsync(catalogSource, cancellationToken);
            }
            catch (Exception exception) when (exception is TemplatePackageException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return new CatalogStatusResult("catalog-unreadable", null, catalogSource);
            }

            catalogCache[catalogSource] = catalog;
        }

        var entry = catalog?.Templates.SingleOrDefault(
            item => string.Equals(item.Id, template.Id, StringComparison.Ordinal));

        if (entry is null)
        {
            return new CatalogStatusResult("not-in-catalog", null, catalogSource);
        }

        var status = string.Equals(entry.Version, template.Version, StringComparison.Ordinal)
            ? "current"
            : "catalog-version-differs";

        return new CatalogStatusResult(status, entry.Version, catalogSource);
    }

    private static string ResolveLocalSource(string projectRoot, string source)
    {
        var expandedSource = source.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                source[2..])
            : source;

        return Path.GetFullPath(Path.IsPathRooted(expandedSource)
            ? expandedSource
            : Path.Combine(projectRoot, expandedSource));
    }

    private static JsonArray CreateExportArray(
        IReadOnlyList<string> appliedExports,
        IReadOnlyList<string> skippedExports,
        IReadOnlyDictionary<string, FabricatorProjectIntegrationPoint> integrationPoints)
    {
        var exports = new JsonArray();

        foreach (var appliedExport in appliedExports)
        {
            exports.Add(CreateExportObject(appliedExport, "added", integrationPoints));
        }

        foreach (var skippedExport in skippedExports)
        {
            exports.Add(CreateExportObject(skippedExport, "already-exists", integrationPoints));
        }

        return exports;
    }

    private static JsonObject CreateExportObject(
        string export,
        string result,
        IReadOnlyDictionary<string, FabricatorProjectIntegrationPoint> integrationPoints)
    {
        var parsedExport = ParseExport(export);
        var path = parsedExport.IntegrationPoint is not null &&
                   integrationPoints.TryGetValue(parsedExport.IntegrationPoint, out var integrationPoint)
            ? integrationPoint.Path
            : string.Empty;

        return new JsonObject
        {
            ["integrationPoint"] = parsedExport.IntegrationPoint ?? string.Empty,
            ["path"] = path,
            ["statement"] = parsedExport.Statement,
            ["result"] = result
        };
    }

    private static (string? IntegrationPoint, string Statement) ParseExport(string export)
    {
        var separatorIndex = export.IndexOf(": ", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return (null, export);
        }

        return (
            export[..separatorIndex],
            export[(separatorIndex + 2)..]);
    }

    private static JsonArray CreateIntegrationNoteArray(
        IReadOnlyList<FabricatorTemplateIntegrationReport> reports)
    {
        var notes = new JsonArray();

        foreach (var report in reports)
        {
            notes.Add(new JsonObject
            {
                ["type"] = report.Kind,
                ["target"] = report.Target ?? string.Empty,
                ["message"] = report.Message
            });
        }

        return notes;
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

    private static StateReadResult TryReadState(string statePath)
    {
        if (!File.Exists(statePath))
        {
            return StateReadResult.Failed(
                [$"Fabricator project state file was not found: {statePath}. Run `rn-fabricator create` or add a valid fabricator.json before applying templates."]);
        }

        try
        {
            var state = JsonNode.Parse(File.ReadAllText(statePath)) as JsonObject;
            if (state is null)
            {
                return StateReadResult.Failed(
                    [$"Fabricator project state file could not be read as an object: {statePath}"]);
            }

            var errors = ValidateState(state, statePath);
            if (errors.Count != 0)
            {
                return StateReadResult.Failed(errors);
            }

            return StateReadResult.Success(state, state["appliedTemplates"]!.AsArray());
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return StateReadResult.Failed(
                [$"Fabricator project state file could not be read: {statePath}. {exception.Message}"]);
        }
    }

    private static IReadOnlyList<string> ValidateState(JsonObject state, string statePath)
    {
        var errors = new List<string>();

        if (state["schemaVersion"]?.GetValue<int>() != FabricatorProjectStateContract.CurrentSchemaVersion)
        {
            errors.Add($"Fabricator project state schema is not supported: {statePath}");
        }

        if (!string.Equals(
            state["kind"]?.GetValue<string>(),
            FabricatorProjectStateContract.ProjectStateKind,
            StringComparison.Ordinal))
        {
            errors.Add($"Fabricator project state kind is invalid: {statePath}");
        }

        if (state["appliedTemplates"] is not JsonArray)
        {
            errors.Add($"Fabricator project state must contain an appliedTemplates array: {statePath}");
        }

        return errors;
    }

    private static bool IsRemoteSource(string source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
               (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record StateReadResult(
        JsonObject? State,
        JsonArray? AppliedTemplates,
        IReadOnlyList<string> Errors)
    {
        public static StateReadResult Success(JsonObject state, JsonArray appliedTemplates)
        {
            return new StateReadResult(state, appliedTemplates, []);
        }

        public static StateReadResult Failed(IReadOnlyList<string> errors)
        {
            return new StateReadResult(null, null, errors);
        }
    }

    private sealed record CatalogStatusResult(
        string Status,
        string? Version,
        string? Source);
}
