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

    private static string Missing(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Missing"
            : value;
    }
}
