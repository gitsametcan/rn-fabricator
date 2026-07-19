using System.Text.Json;

namespace Fabricator.Core.Workspaces;

public sealed class ApplicationCommandCenterMetadataService : IApplicationCommandCenterMetadataService
{
    public const string MetadataRelativePath = ".fabricator/app-command-center.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

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

    public ApplicationCommandCenterMetadataWriteResult Write(
        string projectPath,
        ApplicationCommandCenterMetadata? metadata)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return ApplicationCommandCenterMetadataWriteResult.Failed(
                string.Empty,
                string.Empty,
                ["Project path is required."]);
        }

        string fullProjectPath;
        string metadataPath;
        try
        {
            fullProjectPath = Path.GetFullPath(projectPath);
            metadataPath = Path.Combine(fullProjectPath, MetadataRelativePath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ApplicationCommandCenterMetadataWriteResult.Failed(
                projectPath,
                string.Empty,
                [$"Project path is invalid: {exception.Message}"]);
        }

        if (!Directory.Exists(fullProjectPath))
        {
            return ApplicationCommandCenterMetadataWriteResult.Failed(
                fullProjectPath,
                metadataPath,
                ["Project path does not exist."]);
        }

        if (metadata is null)
        {
            return ApplicationCommandCenterMetadataWriteResult.Failed(
                fullProjectPath,
                metadataPath,
                ["Application command center metadata is required."]);
        }

        if (File.Exists(metadataPath))
        {
            var existing = Read(fullProjectPath);
            if (!existing.IsValid)
            {
                return ApplicationCommandCenterMetadataWriteResult.Failed(
                    fullProjectPath,
                    metadataPath,
                    existing.Errors);
            }
        }

        var normalizedMetadata = Normalize(metadata);
        var errors = Validate(normalizedMetadata);
        if (errors.Count > 0)
        {
            return ApplicationCommandCenterMetadataWriteResult.Failed(
                fullProjectPath,
                metadataPath,
                errors);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            var contents = JsonSerializer.Serialize(normalizedMetadata, SerializerOptions);
            File.WriteAllText(metadataPath, contents + System.Environment.NewLine);

            return ApplicationCommandCenterMetadataWriteResult.Written(
                fullProjectPath,
                metadataPath,
                normalizedMetadata);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return ApplicationCommandCenterMetadataWriteResult.Failed(
                fullProjectPath,
                metadataPath,
                [$"Application command center metadata could not be written: {exception.Message}"]);
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

    private static ApplicationCommandCenterMetadata Normalize(ApplicationCommandCenterMetadata metadata)
    {
        var publishing = metadata.Publishing ?? new ApplicationPublishingMetadata();

        return metadata with
        {
            Publishing = publishing with
            {
                Ios = publishing.Ios ?? new ApplicationPlatformPublishingMetadata(),
                Android = publishing.Android ?? new ApplicationPlatformPublishingMetadata()
            },
            MarketResearch = Normalize(metadata.MarketResearch),
            ProjectIntelligence = Normalize(metadata.ProjectIntelligence),
            ReleaseChecklist = Normalize(metadata.ReleaseChecklist),
            NextActions = NormalizeCollection(metadata.NextActions)
        };
    }

    private static ApplicationMarketResearchMetadata Normalize(ApplicationMarketResearchMetadata? metadata)
    {
        metadata ??= new ApplicationMarketResearchMetadata();

        return metadata with
        {
            Keywords = NormalizeCollection(metadata.Keywords),
            Competitors = NormalizeCollection(metadata.Competitors),
            OpenQuestions = NormalizeCollection(metadata.OpenQuestions)
        };
    }

    private static ApplicationProjectIntelligenceMetadata Normalize(ApplicationProjectIntelligenceMetadata? metadata)
    {
        metadata ??= new ApplicationProjectIntelligenceMetadata();

        return metadata with
        {
            MetricSnapshots = NormalizeCollection(metadata.MetricSnapshots),
            MilestoneProgress = NormalizeCollection(metadata.MilestoneProgress),
            Assumptions = NormalizeCollection(metadata.Assumptions)
        };
    }

    private static ApplicationReleaseChecklistMetadata Normalize(ApplicationReleaseChecklistMetadata? metadata)
    {
        metadata ??= new ApplicationReleaseChecklistMetadata();

        return metadata with
        {
            Items = NormalizeCollection(metadata.Items)
        };
    }

    private static IReadOnlyList<T> NormalizeCollection<T>(IReadOnlyList<T>? values)
    {
        return values ?? [];
    }
}
