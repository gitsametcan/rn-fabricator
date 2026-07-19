namespace Fabricator.Core.Workspaces;

public sealed record ApplicationCommandCenterMetadata
{
    public const int CurrentSchemaVersion = 1;

    public const string KindValue = "fabricator-app-command-center";

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Kind { get; init; } = KindValue;

    public ApplicationPublishingMetadata Publishing { get; init; } = new();

    public ApplicationMarketResearchMetadata MarketResearch { get; init; } = new();

    public ApplicationProjectIntelligenceMetadata ProjectIntelligence { get; init; } = new();

    public ApplicationReleaseChecklistMetadata ReleaseChecklist { get; init; } = new();

    public IReadOnlyList<ApplicationNextActionMetadata> NextActions { get; init; } = [];

    public static ApplicationCommandCenterMetadata Empty { get; } = new();
}

public sealed record ApplicationPublishingMetadata
{
    public ApplicationPlatformPublishingMetadata Ios { get; init; } = new();

    public ApplicationPlatformPublishingMetadata Android { get; init; } = new();

    public string? ReleaseOwner { get; init; }

    public string? Notes { get; init; }
}

public sealed record ApplicationPlatformPublishingMetadata
{
    public string? DisplayName { get; init; }

    public string? Identifier { get; init; }

    public string? Version { get; init; }

    public string? BuildNumber { get; init; }

    public string? StoreUrl { get; init; }

    public string? Status { get; init; }

    public string? Notes { get; init; }
}

public sealed record ApplicationMarketResearchMetadata
{
    public string? TargetAudience { get; init; }

    public string? Positioning { get; init; }

    public IReadOnlyList<string> Keywords { get; init; } = [];

    public IReadOnlyList<string> Competitors { get; init; } = [];

    public IReadOnlyList<string> OpenQuestions { get; init; } = [];

    public string? Notes { get; init; }
}

public sealed record ApplicationProjectIntelligenceMetadata
{
    public int? TargetUsers { get; init; }

    public DateOnly? TargetDate { get; init; }

    public string? ReportingCadence { get; init; }

    public IReadOnlyList<ApplicationProjectMetricSnapshotMetadata> MetricSnapshots { get; init; } = [];

    public IReadOnlyList<ApplicationProductMilestoneMetadata> MilestoneProgress { get; init; } = [];

    public IReadOnlyList<string> Assumptions { get; init; } = [];

    public string? Notes { get; init; }
}

public sealed record ApplicationProjectMetricSnapshotMetadata
{
    public DateOnly Date { get; init; }

    public int AcquiredUsers { get; init; }

    public int ActiveUsers { get; init; }

    public decimal? RetentionProxy { get; init; }

    public string? Notes { get; init; }
}

public sealed record ApplicationProductMilestoneMetadata
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Status { get; init; } = "unknown";

    public int? ProgressPercent { get; init; }

    public string? Notes { get; init; }
}

public sealed record ApplicationReleaseChecklistMetadata
{
    public IReadOnlyList<ApplicationReleaseChecklistItemMetadata> Items { get; init; } = [];

    public string? Notes { get; init; }
}

public sealed record ApplicationReleaseChecklistItemMetadata
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Status { get; init; } = "unknown";

    public string? Notes { get; init; }
}

public sealed record ApplicationNextActionMetadata
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Group { get; init; } = "general";

    public string Status { get; init; } = "open";

    public string? Notes { get; init; }
}
