using Fabricator.Core.Projects;
using Fabricator.Core.Templates;
using Fabricator.Core.Workspaces;

namespace Fabricator.Tests.Workspaces;

public sealed class WorkspaceDiscoveryServiceTests
{
    [Fact]
    public void DiscoverReturnsMissingWorkspaceStateWhenDirectoryDoesNotExist()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"fabricator-missing-{Guid.NewGuid():N}");
        var service = new WorkspaceDiscoveryService();

        var result = service.Discover(workspacePath);

        Assert.False(result.WorkspaceExists);
        Assert.Equal(Path.GetFullPath(workspacePath), result.WorkspacePath);
        Assert.False(result.TemplateCatalog.Exists);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(workspacePath, TemplateSourceResolver.ConventionalCatalogRelativePath)),
            result.TemplateCatalog.Path);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public void DiscoverDetectsConventionalTemplateCatalog()
    {
        using var workspace = new TemporaryDirectory();
        var catalogPath = Path.Combine(workspace.Path, TemplateSourceResolver.ConventionalCatalogRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(catalogPath)!);
        File.WriteAllText(catalogPath, "{}");
        var service = new WorkspaceDiscoveryService();

        var result = service.Discover(workspace.Path);

        Assert.True(result.WorkspaceExists);
        Assert.True(result.TemplateCatalog.Exists);
        Assert.Equal(Path.GetFullPath(catalogPath), result.TemplateCatalog.Path);
    }

    [Fact]
    public void DiscoverClassifiesFabricatorCompatibleProjects()
    {
        using var workspace = new TemporaryDirectory();
        var projectPath = Path.Combine(workspace.Path, "FabricatorApp");
        Directory.CreateDirectory(Path.Combine(projectPath, ".fabricator"));
        File.WriteAllText(Path.Combine(projectPath, FabricatorProjectContract.ManifestRelativePath), "{}");
        File.WriteAllText(Path.Combine(projectPath, FabricatorProjectStateContract.StateRelativePath), "{}");
        File.WriteAllText(Path.Combine(projectPath, "package.json"), PackageJson(hasReactNative: true));
        var service = new WorkspaceDiscoveryService();

        var result = service.Discover(workspace.Path);

        var project = Assert.Single(result.Projects);
        Assert.Equal("FabricatorApp", project.Name);
        Assert.Equal(Path.GetFullPath(projectPath), project.Path);
        Assert.Equal(WorkspaceProjectKind.FabricatorCompatible, project.Kind);
        Assert.True(project.HasFabricatorManifest);
        Assert.True(project.HasFabricatorState);
        Assert.True(project.HasPackageJson);
    }

    [Fact]
    public void DiscoverKeepsReactNativeProjectsMissingFabricatorStateVisible()
    {
        using var workspace = new TemporaryDirectory();
        var projectPath = Path.Combine(workspace.Path, "ExistingMobileApp");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "package.json"), PackageJson(hasReactNative: true));
        var service = new WorkspaceDiscoveryService();

        var result = service.Discover(workspace.Path);

        var project = Assert.Single(result.Projects);
        Assert.Equal("ExistingMobileApp", project.Name);
        Assert.Equal(WorkspaceProjectKind.ReactNativeMissingFabricatorState, project.Kind);
        Assert.False(project.HasFabricatorManifest);
        Assert.False(project.HasFabricatorState);
        Assert.True(project.HasPackageJson);
    }

    [Fact]
    public void DiscoverUsesAppAndNativeFoldersAsReactNativeSignals()
    {
        using var workspace = new TemporaryDirectory();
        var appJsonProjectPath = Path.Combine(workspace.Path, "AppJsonMobileApp");
        Directory.CreateDirectory(appJsonProjectPath);
        File.WriteAllText(Path.Combine(appJsonProjectPath, "app.json"), "{}");

        var nativeProjectPath = Path.Combine(workspace.Path, "NativeMobileApp");
        Directory.CreateDirectory(Path.Combine(nativeProjectPath, "ios"));
        Directory.CreateDirectory(Path.Combine(nativeProjectPath, "android"));
        var service = new WorkspaceDiscoveryService();

        var result = service.Discover(workspace.Path);

        Assert.Equal(2, result.Projects.Count);
        Assert.All(result.Projects, project =>
            Assert.Equal(WorkspaceProjectKind.ReactNativeMissingFabricatorState, project.Kind));
        Assert.Contains(result.Projects, project => project.Name == "AppJsonMobileApp" && project.HasAppJson);
        Assert.Contains(result.Projects, project => project.Name == "NativeMobileApp" && project.HasIosDirectory && project.HasAndroidDirectory);
    }

    [Fact]
    public void DiscoverSkipsFoldersWithoutMobileProjectSignals()
    {
        using var workspace = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(workspace.Path, "templates"));
        Directory.CreateDirectory(Path.Combine(workspace.Path, "notes"));
        var nodeOnlyPath = Path.Combine(workspace.Path, "NodeOnly");
        Directory.CreateDirectory(nodeOnlyPath);
        File.WriteAllText(Path.Combine(nodeOnlyPath, "package.json"), PackageJson(hasReactNative: false));
        var service = new WorkspaceDiscoveryService();

        var result = service.Discover(workspace.Path);

        Assert.Empty(result.Projects);
    }

    [Fact]
    public void DiscoverSortsProjectsByFolderName()
    {
        using var workspace = new TemporaryDirectory();
        CreateReactNativeProject(workspace.Path, "Zeta");
        CreateReactNativeProject(workspace.Path, "Alpha");
        CreateReactNativeProject(workspace.Path, "Beta");
        var service = new WorkspaceDiscoveryService();

        var result = service.Discover(workspace.Path);

        Assert.Equal(["Alpha", "Beta", "Zeta"], result.Projects.Select(project => project.Name));
    }

    private static void CreateReactNativeProject(string workspacePath, string name)
    {
        var projectPath = Path.Combine(workspacePath, name);
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "package.json"), PackageJson(hasReactNative: true));
    }

    private static string PackageJson(bool hasReactNative)
    {
        return hasReactNative
            ? """
              {
                "dependencies": {
                  "react-native": "0.76.0"
                }
              }
              """
            : """
              {
                "dependencies": {
                  "left-pad": "1.3.0"
                }
              }
              """;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"fabricator-workspace-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
