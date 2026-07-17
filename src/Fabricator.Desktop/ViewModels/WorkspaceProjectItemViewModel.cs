using Fabricator.Core.Workspaces;

namespace Fabricator.Desktop.ViewModels;

public sealed class WorkspaceProjectItemViewModel
{
    public WorkspaceProjectItemViewModel(WorkspaceProjectCandidate candidate)
    {
        Name = candidate.Name;
        Path = candidate.Path;
        Status = candidate.Kind == WorkspaceProjectKind.FabricatorCompatible
            ? "Ready"
            : "Missing Fabricator state";
        Details = candidate.Kind == WorkspaceProjectKind.FabricatorCompatible
            ? "Fabricator-compatible React Native project"
            : "React Native project without Fabricator state";
        HasFabricatorState = candidate.HasFabricatorManifest || candidate.HasFabricatorState;
        SignalSummary = CreateSignalSummary(candidate);
    }

    public string Name { get; }

    public string Path { get; }

    public string Status { get; }

    public string Details { get; }

    public bool HasFabricatorState { get; }

    public string SignalSummary { get; }

    private static string CreateSignalSummary(WorkspaceProjectCandidate candidate)
    {
        var signals = new List<string>();

        if (candidate.HasPackageJson)
        {
            signals.Add("package.json");
        }

        if (candidate.HasAppJson)
        {
            signals.Add("app.json");
        }

        if (candidate.HasIosDirectory)
        {
            signals.Add("ios");
        }

        if (candidate.HasAndroidDirectory)
        {
            signals.Add("android");
        }

        if (candidate.HasFabricatorManifest)
        {
            signals.Add(".fabricator/project.json");
        }

        if (candidate.HasFabricatorState)
        {
            signals.Add("fabricator.json");
        }

        return signals.Count == 0
            ? "No recognized signals"
            : string.Join(", ", signals);
    }
}
