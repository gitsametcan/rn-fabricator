using Fabricator.Core.Templates;

namespace Fabricator.Tests.Templates;

public sealed class BasicAuthTemplateAssetsTests
{
    private static readonly string[] ExpectedScreenFiles =
    [
        "src/screens/SplashScreen.tsx",
        "src/screens/LoadingScreen.tsx",
        "src/screens/LoginScreen.tsx",
        "src/screens/HomeScreen.tsx",
        "src/screens/index.ts"
    ];

    [Fact]
    public void BasicAuthManifestListsExpectedScreenFiles()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");

        Assert.Equal(ExpectedScreenFiles, package.Manifest.Files);
    }

    [Theory]
    [InlineData("src/screens/SplashScreen.tsx", "SplashScreen")]
    [InlineData("src/screens/LoadingScreen.tsx", "LoadingScreen")]
    [InlineData("src/screens/LoginScreen.tsx", "LoginScreen")]
    [InlineData("src/screens/HomeScreen.tsx", "HomeScreen")]
    public void BasicAuthScreensExistAndExportNamedComponents(string relativePath, string componentName)
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        var screenPath = Path.Combine(package.RootDirectory, relativePath);

        var content = File.ReadAllText(screenPath);

        Assert.Contains($"export function {componentName}", content);
        Assert.Contains("StyleSheet.create", content);
        Assert.DoesNotContain("client_secret", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api_key", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BasicAuthScreenIndexExportsEveryScreen()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        var indexPath = Path.Combine(package.RootDirectory, "src/screens/index.ts");

        var content = File.ReadAllText(indexPath);

        Assert.Contains("HomeScreen", content);
        Assert.Contains("LoadingScreen", content);
        Assert.Contains("LoginScreen", content);
        Assert.Contains("SplashScreen", content);
    }
}
