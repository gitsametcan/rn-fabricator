using Fabricator.Core.Workspaces;

namespace Fabricator.Desktop.ViewModels;

public sealed class WorkspaceProjectDetailViewModel
{
    public WorkspaceProjectDetailViewModel(WorkspaceProjectDetail detail)
    {
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
}
