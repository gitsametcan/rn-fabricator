using Fabricator.Core.Projects;

namespace Fabricator.Tests.Projects;

public sealed class FabricatorProjectContractTests
{
    [Fact]
    public void CreateManifestUsesCurrentProjectContract()
    {
        var manifest = FabricatorProjectContract.CreateManifest("0.9.0");

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal("fabricator-react-native-project", manifest.Kind);
        Assert.Equal("0.9.0", manifest.FabricatorVersion);
        Assert.Equal("react-native-cli", manifest.ProjectType);
        Assert.Equal("src", manifest.SourceRoot);
    }

    [Fact]
    public void CreateDefaultFoldersReturnsFabricatorSourceLayout()
    {
        var folders = FabricatorProjectContract.CreateDefaultFolders();

        Assert.Collection(
            folders,
            folder => Assert.Equal(("app", "src/app"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("components", "src/components"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("config", "src/config"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("constants", "src/constants"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("hooks", "src/hooks"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("navigation", "src/navigation"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("screens", "src/screens"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("services", "src/services"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("storage", "src/storage"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("theme", "src/theme"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("types", "src/types"), (folder.Key, folder.Path)),
            folder => Assert.Equal(("utils", "src/utils"), (folder.Key, folder.Path)));
    }

    [Fact]
    public void CreateDefaultIntegrationPointsReturnsSafeBarrelExports()
    {
        var integrationPoints = FabricatorProjectContract.CreateDefaultIntegrationPoints();

        Assert.Collection(
            integrationPoints,
            point => Assert.Equal(("screensBarrel", "src/screens/index.ts", "barrel-export"), (point.Key, point.Path, point.Type)),
            point => Assert.Equal(("componentsBarrel", "src/components/index.ts", "barrel-export"), (point.Key, point.Path, point.Type)),
            point => Assert.Equal(("servicesBarrel", "src/services/index.ts", "barrel-export"), (point.Key, point.Path, point.Type)));
    }
}
