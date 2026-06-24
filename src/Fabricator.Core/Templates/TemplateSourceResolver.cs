using Fabricator.Core.Environment;
using Fabricator.Core.Projects;
using System.Text.Json;

namespace Fabricator.Core.Templates;

public sealed class TemplateSourceResolver : ITemplateSourceResolver
{
    public const string EnvironmentVariableName = "RN_FABRICATOR_TEMPLATE_SOURCE";
    public const string ConventionalCatalogRelativePath = "templates/catalog.fabricator.json";

    private readonly IEnvironmentVariables _environmentVariables;

    public TemplateSourceResolver()
        : this(new SystemEnvironmentVariables())
    {
    }

    public TemplateSourceResolver(IEnvironmentVariables environmentVariables)
    {
        _environmentVariables = environmentVariables;
    }

    public TemplateSourceResolutionResult Resolve(TemplateSourceResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkingDirectory);

        if (!string.IsNullOrWhiteSpace(request.ExplicitSource))
        {
            return TemplateSourceResolutionResult.Resolved(
                request.ExplicitSource,
                "explicit --source");
        }

        var environmentSource = _environmentVariables.Get(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentSource))
        {
            return TemplateSourceResolutionResult.Resolved(
                ResolveSourceValue(environmentSource, request.WorkingDirectory),
                EnvironmentVariableName);
        }

        var stateSource = ResolveFromProjectState(request);
        if (stateSource is not null)
        {
            return stateSource;
        }

        var conventionalCatalog = Path.GetFullPath(
            Path.Combine(request.WorkingDirectory, ConventionalCatalogRelativePath));
        if (File.Exists(conventionalCatalog))
        {
            return TemplateSourceResolutionResult.Resolved(
                conventionalCatalog,
                ConventionalCatalogRelativePath);
        }

        return TemplateSourceResolutionResult.Failed(
            "Template source could not be resolved. Pass --source <catalog-url-or-path>, set RN_FABRICATOR_TEMPLATE_SOURCE, configure templateSources in fabricator.json, or add ./templates/catalog.fabricator.json.");
    }

    private static TemplateSourceResolutionResult? ResolveFromProjectState(
        TemplateSourceResolutionRequest request)
    {
        foreach (var stateDirectory in GetStateSearchDirectories(request))
        {
            var statePath = Path.Combine(stateDirectory, FabricatorProjectStateContract.StateRelativePath);
            if (!File.Exists(statePath))
            {
                continue;
            }

            var source = TryReadDefaultCatalogSource(statePath);
            if (source is null)
            {
                continue;
            }

            var sourceValue = source.Value;

            return TemplateSourceResolutionResult.Resolved(
                ResolveSourceValue(sourceValue.Value, stateDirectory),
                $"fabricator.json:{sourceValue.Name}");
        }

        return null;
    }

    private static IEnumerable<string> GetStateSearchDirectories(TemplateSourceResolutionRequest request)
    {
        var yielded = new HashSet<string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(request.ProjectDirectory))
        {
            var projectDirectory = Path.GetFullPath(request.ProjectDirectory);
            if (yielded.Add(projectDirectory))
            {
                yield return projectDirectory;
            }
        }

        var workingDirectory = Path.GetFullPath(request.WorkingDirectory);
        if (yielded.Add(workingDirectory))
        {
            yield return workingDirectory;
        }
    }

    private static (string Name, string Value)? TryReadDefaultCatalogSource(string statePath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(statePath));
            var root = document.RootElement;

            if (!root.TryGetProperty("templateSources", out var sources) ||
                sources.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var catalogSources = new List<(string Name, string Type, string Value, bool IsDefault)>();

            foreach (var sourceElement in sources.EnumerateArray())
            {
                var source = ReadCatalogSource(sourceElement);
                if (source is not null)
                {
                    catalogSources.Add(source.Value);
                }
            }

            var defaultSource = catalogSources.FirstOrDefault(source => source.IsDefault);
            if (!string.IsNullOrWhiteSpace(defaultSource.Value))
            {
                return (defaultSource.Name, defaultSource.Value);
            }

            var firstSource = catalogSources.FirstOrDefault();
            return string.IsNullOrWhiteSpace(firstSource.Value)
                ? null
                : (firstSource.Name, firstSource.Value);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static (string Name, string Type, string Value, bool IsDefault)? ReadCatalogSource(JsonElement source)
    {
        var type = ReadString(source, "type");
        var value = ReadString(source, "value");

        if (string.IsNullOrWhiteSpace(type) ||
            string.IsNullOrWhiteSpace(value) ||
            string.Equals(type, FabricatorProjectStateContract.SourceTypeEmbedded, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.Equals(type, FabricatorProjectStateContract.SourceTypeLocal, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(type, FabricatorProjectStateContract.SourceTypeRemote, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var name = ReadString(source, "name");
        var isDefault = source.TryGetProperty("isDefault", out var isDefaultElement) &&
                        isDefaultElement.ValueKind == JsonValueKind.True;

        return (
            string.IsNullOrWhiteSpace(name) ? type : name,
            type,
            value,
            isDefault);
    }

    private static string? ReadString(JsonElement source, string propertyName)
    {
        return source.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string ResolveSourceValue(string source, string baseDirectory)
    {
        return IsRemoteSource(source) || Path.IsPathFullyQualified(source)
            ? source
            : Path.GetFullPath(Path.Combine(baseDirectory, source));
    }

    private static bool IsRemoteSource(string source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
               (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }
}
