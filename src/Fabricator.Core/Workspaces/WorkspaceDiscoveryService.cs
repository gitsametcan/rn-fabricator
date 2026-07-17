using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using System.Text.Json;

namespace Fabricator.Core.Workspaces;

public sealed class WorkspaceDiscoveryService : IWorkspaceDiscoveryService
{
    private const string ReactNativePackageName = "react-native";

    public WorkspaceDiscoveryResult Discover(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var fullWorkspacePath = Path.GetFullPath(workspacePath);
        var catalogPath = Path.GetFullPath(Path.Combine(
            fullWorkspacePath,
            TemplateSourceResolver.ConventionalCatalogRelativePath));
        var catalog = new WorkspaceTemplateCatalog(catalogPath, File.Exists(catalogPath));

        if (!Directory.Exists(fullWorkspacePath))
        {
            return new WorkspaceDiscoveryResult(
                fullWorkspacePath,
                WorkspaceExists: false,
                catalog,
                []);
        }

        var projects = Directory
            .EnumerateDirectories(fullWorkspacePath)
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .Select(InspectProjectCandidate)
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .ToArray();

        return new WorkspaceDiscoveryResult(
            fullWorkspacePath,
            WorkspaceExists: true,
            catalog,
            projects);
    }

    private static WorkspaceProjectCandidate? InspectProjectCandidate(string projectPath)
    {
        var hasFabricatorManifest = File.Exists(Path.Combine(
            projectPath,
            FabricatorProjectContract.ManifestRelativePath));
        var hasFabricatorState = File.Exists(Path.Combine(
            projectPath,
            FabricatorProjectStateContract.StateRelativePath));
        var hasPackageJson = File.Exists(Path.Combine(projectPath, "package.json"));
        var hasAppJson = File.Exists(Path.Combine(projectPath, "app.json"));
        var hasIosDirectory = Directory.Exists(Path.Combine(projectPath, "ios"));
        var hasAndroidDirectory = Directory.Exists(Path.Combine(projectPath, "android"));
        var isFabricatorCompatible = hasFabricatorManifest || hasFabricatorState;
        var looksLikeReactNative = hasAppJson ||
                                   (hasIosDirectory && hasAndroidDirectory) ||
                                   PackageReferencesReactNative(Path.Combine(projectPath, "package.json"));

        var kind = isFabricatorCompatible
            ? WorkspaceProjectKind.FabricatorCompatible
            : looksLikeReactNative
                ? WorkspaceProjectKind.ReactNativeMissingFabricatorState
                : (WorkspaceProjectKind?)null;

        if (kind is null)
        {
            return null;
        }

        return new WorkspaceProjectCandidate(
            Path.GetFileName(projectPath),
            Path.GetFullPath(projectPath),
            kind.Value,
            hasFabricatorManifest,
            hasFabricatorState,
            hasPackageJson,
            hasAppJson,
            hasIosDirectory,
            hasAndroidDirectory);
    }

    private static bool PackageReferencesReactNative(string packageJsonPath)
    {
        if (!File.Exists(packageJsonPath))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
            var root = document.RootElement;

            return ObjectHasPackage(root, "dependencies", ReactNativePackageName) ||
                   ObjectHasPackage(root, "devDependencies", ReactNativePackageName);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    private static bool ObjectHasPackage(JsonElement root, string propertyName, string packageName)
    {
        return root.TryGetProperty(propertyName, out var dependencies) &&
               dependencies.ValueKind == JsonValueKind.Object &&
               dependencies.TryGetProperty(packageName, out _);
    }
}
