using Fabricator.Core.Workspaces;

namespace Fabricator.Desktop.ViewModels;

public sealed class WorkspaceProjectDetailViewModel
{
    public WorkspaceProjectDetailViewModel(WorkspaceProjectDetail detail)
    {
        var projectIntelligence = detail.CommandCenterMetadata.Metadata.ProjectIntelligence ?? new ApplicationProjectIntelligenceMetadata();

        DisplayName = detail.DisplayName;
        ProjectPath = detail.ProjectPath;
        PackageName = Missing(detail.PackageName);
        FabricatorStatus = detail.HasFabricatorManifest || detail.HasFabricatorState
            ? "Fabricator-compatible"
            : "Missing Fabricator state";
        AppliedTemplateCount = $"{detail.AppliedTemplateCount} applied template(s)";
        SourceFileCount = $"{detail.Statistics.SourceFileCount} source file(s)";
        ScreenFileCount = $"{detail.Statistics.ScreenFileCount} screen file(s)";
        ComponentFileCount = $"{detail.Statistics.ComponentFileCount} component file(s)";
        ServiceFileCount = $"{detail.Statistics.ServiceFileCount} service file(s)";
        UtilityFileCount = $"{detail.Statistics.UtilityFileCount} utility file(s)";
        AppStoreName = Missing(detail.StoreMetadata.AppStoreName);
        PlayStoreName = Missing(detail.StoreMetadata.PlayStoreName);
        IosBundleIdentifier = Missing(detail.StoreMetadata.IosBundleIdentifier);
        AndroidApplicationId = Missing(detail.StoreMetadata.AndroidApplicationId);
        AppVersion = Missing(detail.StoreMetadata.AppVersion);
        BuildNumber = Missing(detail.StoreMetadata.BuildNumber);
        AgentMemoryStatus = detail.AgentMemory.HasRootInstructions || detail.AgentMemory.HasAgentsDirectory
            ? "Configured"
            : "Not configured";
        AgentInstructionsStatus = detail.AgentMemory.HasRootInstructions
            ? "AGENTS.md found"
            : "AGENTS.md missing";
        AgentDirectoryStatus = detail.AgentMemory.HasAgentsDirectory
            ? ".agents found"
            : ".agents missing";
        AgentHandoffStatus = detail.AgentMemory.HasHandoff
            ? $"handoff.md found{FormatLastModified(detail.AgentMemory.HandoffLastModified)}"
            : "handoff.md missing";
        AgentCurrentFocusStatus = detail.AgentMemory.HasCurrentFocus
            ? "current-focus.md found"
            : "current-focus.md missing";
        AgentCurrentFocusSummary = Missing(detail.AgentMemory.CurrentFocusSummary);
        AgentOpenQuestionCount = $"{detail.AgentMemory.OpenQuestionCount} open question(s)";
        HasCommandCenterMetadata = detail.CommandCenterMetadata.HasMetadata;
        CanCreateCommandCenterMetadata = !detail.CommandCenterMetadata.Exists;
        CommandCenterStatus = BuildCommandCenterStatus(detail.CommandCenterMetadata);
        CommandCenterMetadataPath = detail.CommandCenterMetadata.MetadataPath;
        PublishingSummary = detail.CommandCenterMetadata.HasMetadata
            ? BuildPublishingSummary(detail.CommandCenterMetadata.Metadata.Publishing)
            : "No publishing metadata yet.";
        PublishingReadinessItems = BuildPublishingReadinessItems(
            detail.StoreMetadata,
            detail.CommandCenterMetadata.Metadata.Publishing,
            detail.CommandCenterMetadata.HasMetadata);
        ResearchSummary = detail.CommandCenterMetadata.HasMetadata
            ? BuildResearchSummary(detail.CommandCenterMetadata.Metadata.MarketResearch)
            : "No market research notes yet.";
        ResearchItems = BuildResearchItems(detail.CommandCenterMetadata.Metadata.MarketResearch);
        ProjectIntelligenceSummary = detail.CommandCenterMetadata.HasMetadata
            ? BuildProjectIntelligenceSummary(projectIntelligence)
            : "No project intelligence metadata yet.";
        ProjectTargetProgressSummary = BuildProjectTargetProgressSummary(projectIntelligence);
        ProjectTargetProgressValue = BuildProjectTargetProgressValue(projectIntelligence);
        ProjectProjectionSummary = BuildProjectProjectionSummary(projectIntelligence);
        ProjectIntelligenceChartItems = BuildProjectIntelligenceChartItems(projectIntelligence);
        ProjectMilestoneItems = BuildProjectMilestoneItems(projectIntelligence);
        ReleaseChecklistSummary = detail.CommandCenterMetadata.HasMetadata
            ? $"{detail.CommandCenterMetadata.Metadata.ReleaseChecklist.Items.Count} release checklist item(s)"
            : "No release checklist yet.";
        ReleaseChecklistItems = BuildReleaseChecklistItems(detail.CommandCenterMetadata.Metadata.ReleaseChecklist);
        NextActionsSummary = detail.CommandCenterMetadata.HasMetadata
            ? $"{detail.CommandCenterMetadata.Metadata.NextActions.Count} next action(s)"
            : "No next actions yet.";
        NextActionItems = BuildNextActionItems(detail.CommandCenterMetadata.Metadata.NextActions);
    }

    public string DisplayName { get; }

    public string ProjectPath { get; }

    public string PackageName { get; }

    public string FabricatorStatus { get; }

    public string AppliedTemplateCount { get; }

    public string SourceFileCount { get; }

    public string ScreenFileCount { get; }

    public string ComponentFileCount { get; }

    public string ServiceFileCount { get; }

    public string UtilityFileCount { get; }

    public string AppStoreName { get; }

    public string PlayStoreName { get; }

    public string IosBundleIdentifier { get; }

    public string AndroidApplicationId { get; }

    public string AppVersion { get; }

    public string BuildNumber { get; }

    public string AgentMemoryStatus { get; }

    public string AgentInstructionsStatus { get; }

    public string AgentDirectoryStatus { get; }

    public string AgentHandoffStatus { get; }

    public string AgentCurrentFocusStatus { get; }

    public string AgentCurrentFocusSummary { get; }

    public string AgentOpenQuestionCount { get; }

    public string CommandCenterStatus { get; }

    public string CommandCenterMetadataPath { get; }

    public bool HasCommandCenterMetadata { get; }

    public bool CanCreateCommandCenterMetadata { get; }

    public string PublishingSummary { get; }

    public IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> PublishingReadinessItems { get; }

    public string ResearchSummary { get; }

    public IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> ResearchItems { get; }

    public string ProjectIntelligenceSummary { get; }

    public string ProjectTargetProgressSummary { get; }

    public double ProjectTargetProgressValue { get; }

    public string ProjectProjectionSummary { get; }

    public IReadOnlyList<ProjectIntelligenceChartItemViewModel> ProjectIntelligenceChartItems { get; }

    public IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> ProjectMilestoneItems { get; }

    public string ReleaseChecklistSummary { get; }

    public IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> ReleaseChecklistItems { get; }

    public string NextActionsSummary { get; }

    public IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> NextActionItems { get; }

    private static string Missing(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Missing"
            : value;
    }

    private static string FormatLastModified(DateTimeOffset? lastModified)
    {
        return lastModified is null
            ? string.Empty
            : $" ({lastModified.Value.UtcDateTime:yyyy-MM-dd HH:mm} UTC)";
    }

    private static string BuildCommandCenterStatus(ApplicationCommandCenterMetadataReadResult result)
    {
        if (!result.Exists)
        {
            return "Command center metadata missing";
        }

        if (!result.IsValid)
        {
            return $"Command center metadata invalid: {string.Join(" ", result.Errors)}";
        }

        return "Command center metadata loaded";
    }

    private static string BuildPublishingSummary(ApplicationPublishingMetadata publishing)
    {
        var iosStatus = Missing(publishing.Ios.Status);
        var androidStatus = Missing(publishing.Android.Status);

        return $"iOS: {iosStatus}; Android: {androidStatus}";
    }

    private static IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> BuildPublishingReadinessItems(
        WorkspaceStoreMetadata storeMetadata,
        ApplicationPublishingMetadata publishing,
        bool hasCommandCenterMetadata)
    {
        var items = new List<ApplicationCommandCenterPanelItemViewModel>
        {
            BuildItem("iOS display name", publishing.Ios.DisplayName ?? storeMetadata.AppStoreName),
            BuildItem("iOS bundle id", publishing.Ios.Identifier ?? storeMetadata.IosBundleIdentifier),
            BuildItem("Android display name", publishing.Android.DisplayName ?? storeMetadata.PlayStoreName),
            BuildItem("Android application id", publishing.Android.Identifier ?? storeMetadata.AndroidApplicationId),
            BuildItem("App version", publishing.Ios.Version ?? publishing.Android.Version ?? storeMetadata.AppVersion),
            BuildItem("Build number", publishing.Ios.BuildNumber ?? publishing.Android.BuildNumber ?? storeMetadata.BuildNumber),
            BuildItem("Release owner", publishing.ReleaseOwner),
            new(
                "Release metadata",
                hasCommandCenterMetadata ? "Configured" : "Missing",
                hasCommandCenterMetadata
                    ? "Command center metadata found."
                    : "Add .fabricator/app-command-center.json to track release metadata.")
        };

        return items;
    }

    private static ApplicationCommandCenterPanelItemViewModel BuildItem(string label, string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? new ApplicationCommandCenterPanelItemViewModel(label, "Missing", "Not detected.")
            : new ApplicationCommandCenterPanelItemViewModel(label, "Ready", value);
    }

    private static string BuildResearchSummary(ApplicationMarketResearchMetadata research)
    {
        var keywordCount = research.Keywords.Count;
        var competitorCount = research.Competitors.Count;
        var questionCount = research.OpenQuestions.Count;

        return $"{keywordCount} keyword(s), {competitorCount} competitor(s), {questionCount} open question(s)";
    }

    private static IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> BuildResearchItems(
        ApplicationMarketResearchMetadata research)
    {
        return
        [
            BuildItem("Target audience", research.TargetAudience),
            BuildItem("Positioning", research.Positioning),
            BuildCollectionItem("Keywords", research.Keywords),
            BuildCollectionItem("Competitors", research.Competitors),
            BuildCollectionItem("Open questions", research.OpenQuestions),
            BuildItem("Research notes", research.Notes)
        ];
    }

    private static string BuildProjectIntelligenceSummary(ApplicationProjectIntelligenceMetadata intelligence)
    {
        var snapshotCount = intelligence.MetricSnapshots?.Count ?? 0;
        var milestoneCount = intelligence.MilestoneProgress?.Count ?? 0;
        var target = intelligence.TargetUsers is null
            ? "no target"
            : $"{intelligence.TargetUsers.Value} target users";

        return $"{snapshotCount} metric snapshot(s), {milestoneCount} milestone(s), {target}";
    }

    private static string BuildProjectTargetProgressSummary(ApplicationProjectIntelligenceMetadata intelligence)
    {
        var latestSnapshot = LatestSnapshot(intelligence);
        if (latestSnapshot is null)
        {
            return "No metric history yet.";
        }

        if (intelligence.TargetUsers is null || intelligence.TargetUsers.Value <= 0)
        {
            return $"{latestSnapshot.AcquiredUsers} acquired users. No target users yet.";
        }

        var percent = BuildProjectTargetProgressValue(intelligence);
        return $"{latestSnapshot.AcquiredUsers} of {intelligence.TargetUsers.Value} target users ({percent:0.#}%).";
    }

    private static double BuildProjectTargetProgressValue(ApplicationProjectIntelligenceMetadata intelligence)
    {
        var latestSnapshot = LatestSnapshot(intelligence);
        if (latestSnapshot is null || intelligence.TargetUsers is null || intelligence.TargetUsers.Value <= 0)
        {
            return 0;
        }

        return Math.Clamp((double)latestSnapshot.AcquiredUsers / intelligence.TargetUsers.Value * 100, 0, 100);
    }

    private static string BuildProjectProjectionSummary(ApplicationProjectIntelligenceMetadata intelligence)
    {
        var snapshots = OrderedSnapshots(intelligence);
        if (intelligence.TargetUsers is null || intelligence.TargetDate is null)
        {
            return "No target projection yet.";
        }

        if (snapshots.Count == 0)
        {
            return "Insufficient history for projection.";
        }

        var latest = snapshots[^1];
        if (latest.AcquiredUsers >= intelligence.TargetUsers.Value)
        {
            return "Reached: latest acquired users meet the target.";
        }

        if (snapshots.Count < 2)
        {
            return "Insufficient history for projection.";
        }

        if (intelligence.TargetDate.Value <= latest.Date)
        {
            return "At risk: target date is not after the latest metric snapshot.";
        }

        var first = snapshots[0];
        var elapsedPeriods = CountPeriods(first.Date, latest.Date, intelligence.ReportingCadence);
        var remainingPeriods = CountPeriods(latest.Date, intelligence.TargetDate.Value, intelligence.ReportingCadence);
        var currentAverageGrowth = (latest.AcquiredUsers - first.AcquiredUsers) / elapsedPeriods;
        var requiredGrowth = (intelligence.TargetUsers.Value - latest.AcquiredUsers) / remainingPeriods;
        var status = currentAverageGrowth >= requiredGrowth ? "On track" : "At risk";

        return $"{status}: current average growth {currentAverageGrowth:0.#} per period; required {requiredGrowth:0.#} per period.";
    }

    private static IReadOnlyList<ProjectIntelligenceChartItemViewModel> BuildProjectIntelligenceChartItems(
        ApplicationProjectIntelligenceMetadata intelligence)
    {
        var snapshots = OrderedSnapshots(intelligence);
        if (snapshots.Count == 0)
        {
            return
            [
                new ProjectIntelligenceChartItemViewModel(
                    "Metric history",
                    "No local snapshots.",
                    "Add dated snapshots to chart user acquisition and active users.",
                    0,
                    0)
            ];
        }

        var maxAcquired = Math.Max(1, snapshots.Max(snapshot => snapshot.AcquiredUsers));
        return snapshots
            .Select(snapshot => new ProjectIntelligenceChartItemViewModel(
                snapshot.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                $"{snapshot.AcquiredUsers} acquired",
                $"{snapshot.ActiveUsers} active",
                Math.Clamp((double)snapshot.AcquiredUsers / maxAcquired * 100, 0, 100),
                snapshot.AcquiredUsers == 0
                    ? 0
                    : Math.Clamp((double)snapshot.ActiveUsers / snapshot.AcquiredUsers * 100, 0, 100)))
            .ToArray();
    }

    private static IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> BuildProjectMilestoneItems(
        ApplicationProjectIntelligenceMetadata intelligence)
    {
        if (intelligence.MilestoneProgress is null || intelligence.MilestoneProgress.Count == 0)
        {
            return
            [
                new ApplicationCommandCenterPanelItemViewModel(
                    "Product progress",
                    "Missing",
                    "No local milestone progress.")
            ];
        }

        return intelligence.MilestoneProgress
            .Select(milestone => new ApplicationCommandCenterPanelItemViewModel(
                string.IsNullOrWhiteSpace(milestone.Title) ? milestone.Id : milestone.Title,
                Missing(milestone.Status),
                milestone.ProgressPercent is null
                    ? Missing(milestone.Notes)
                    : $"{milestone.ProgressPercent.Value}% complete. {Missing(milestone.Notes)}"))
            .ToArray();
    }

    private static IReadOnlyList<ApplicationProjectMetricSnapshotMetadata> OrderedSnapshots(
        ApplicationProjectIntelligenceMetadata intelligence)
    {
        return (intelligence.MetricSnapshots ?? [])
            .OrderBy(snapshot => snapshot.Date)
            .ToArray();
    }

    private static ApplicationProjectMetricSnapshotMetadata? LatestSnapshot(
        ApplicationProjectIntelligenceMetadata intelligence)
    {
        return OrderedSnapshots(intelligence).LastOrDefault();
    }

    private static double CountPeriods(DateOnly from, DateOnly to, string? cadence)
    {
        var days = Math.Max(1, to.DayNumber - from.DayNumber);
        var divisor = string.Equals(cadence, "monthly", StringComparison.OrdinalIgnoreCase)
            ? 30d
            : 7d;

        return Math.Max(1, days / divisor);
    }

    private static ApplicationCommandCenterPanelItemViewModel BuildCollectionItem(
        string label,
        IReadOnlyList<string> values)
    {
        return values.Count == 0
            ? new ApplicationCommandCenterPanelItemViewModel(label, "Missing", "No local entries.")
            : new ApplicationCommandCenterPanelItemViewModel(label, "Ready", string.Join(", ", values));
    }

    private static IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> BuildReleaseChecklistItems(
        ApplicationReleaseChecklistMetadata releaseChecklist)
    {
        if (releaseChecklist.Items.Count == 0)
        {
            return
            [
                new ApplicationCommandCenterPanelItemViewModel(
                    "Release checklist",
                    "Missing",
                    "No local checklist items.")
            ];
        }

        return releaseChecklist.Items
            .Select(item => new ApplicationCommandCenterPanelItemViewModel(
                string.IsNullOrWhiteSpace(item.Title) ? item.Id : item.Title,
                Missing(item.Status),
                Missing(item.Notes)))
            .ToArray();
    }

    private static IReadOnlyList<ApplicationCommandCenterPanelItemViewModel> BuildNextActionItems(
        IReadOnlyList<ApplicationNextActionMetadata> nextActions)
    {
        if (nextActions.Count == 0)
        {
            return
            [
                new ApplicationCommandCenterPanelItemViewModel(
                    "Next actions",
                    "Missing",
                    "No local next actions.")
            ];
        }

        return nextActions
            .Select(action => new ApplicationCommandCenterPanelItemViewModel(
                string.IsNullOrWhiteSpace(action.Title) ? action.Id : action.Title,
                Missing(action.Status),
                BuildNextActionDetail(action)))
            .ToArray();
    }

    private static string BuildNextActionDetail(ApplicationNextActionMetadata action)
    {
        var group = Missing(action.Group);
        var notes = Missing(action.Notes);

        return notes == "Missing"
            ? $"Group: {group}."
            : $"Group: {group}. {notes}";
    }
}

public sealed record ApplicationCommandCenterPanelItemViewModel(
    string Label,
    string Status,
    string Detail);

public sealed record ProjectIntelligenceChartItemViewModel(
    string Label,
    string AcquiredLabel,
    string ActiveLabel,
    double AcquiredValue,
    double ActiveValue);
