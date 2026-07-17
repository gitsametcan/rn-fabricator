using System.Text.Json;

namespace Fabricator.Core.Workspaces;

public sealed class ApplicationCommandCenterMetadataService : IApplicationCommandCenterMetadataService
{
    public const string MetadataRelativePath = ".fabricator/app-command-center.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public ApplicationCommandCenterMetadataReadResult Read(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var fullProjectPath = Path.GetFullPath(projectPath);
        var metadataPath = Path.Combine(fullProjectPath, MetadataRelativePath);

        if (!File.Exists(metadataPath))
        {
            return ApplicationCommandCenterMetadataReadResult.Missing(fullProjectPath, metadataPath);
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<ApplicationCommandCenterMetadata>(
                File.ReadAllText(metadataPath),
                SerializerOptions);

            if (metadata is null)
            {
                return ApplicationCommandCenterMetadataReadResult.Invalid(
                    fullProjectPath,
                    metadataPath,
                    ["Application command center metadata file is empty."]);
            }

            var errors = Validate(metadata);
            return errors.Count == 0
                ? ApplicationCommandCenterMetadataReadResult.Loaded(fullProjectPath, metadataPath, metadata)
                : ApplicationCommandCenterMetadataReadResult.Invalid(fullProjectPath, metadataPath, errors);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return ApplicationCommandCenterMetadataReadResult.Invalid(
                fullProjectPath,
                metadataPath,
                [$"Application command center metadata could not be read: {exception.Message}"]);
        }
    }

    private static IReadOnlyList<string> Validate(ApplicationCommandCenterMetadata metadata)
    {
        var errors = new List<string>();

        if (metadata.SchemaVersion != ApplicationCommandCenterMetadata.CurrentSchemaVersion)
        {
            errors.Add(
                $"Application command center metadata schema version {metadata.SchemaVersion} is not supported. Expected {ApplicationCommandCenterMetadata.CurrentSchemaVersion}.");
        }

        if (!string.Equals(metadata.Kind, ApplicationCommandCenterMetadata.KindValue, StringComparison.Ordinal))
        {
            errors.Add(
                $"Application command center metadata kind must be {ApplicationCommandCenterMetadata.KindValue}.");
        }

        return errors;
    }
}
