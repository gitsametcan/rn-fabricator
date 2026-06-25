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

    public FabricatorProjectStateService()
        : this(new FabricatorProjectCompatibilityValidator())
    {
    }

    public FabricatorProjectStateService(IFabricatorProjectCompatibilityValidator compatibilityValidator)
    {
        _compatibilityValidator = compatibilityValidator;
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
}
