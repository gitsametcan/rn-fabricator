using Fabricator.Core.Templates;

namespace Fabricator.Tests.Templates;

public sealed class BasicAuthTemplateAssetsTests
{
    private static readonly string[] ExpectedTemplateFiles =
    [
        ".env.example",
        "credentials.example.json",
        "CONFIGURATION.md",
        "App.tsx",
        "src/auth/AuthProvider.tsx",
        "src/auth/index.ts",
        "src/screens/SplashScreen.tsx",
        "src/screens/LoadingScreen.tsx",
        "src/screens/LoginScreen.tsx",
        "src/screens/HomeScreen.tsx",
        "src/screens/index.ts"
    ];

    [Fact]
    public void BasicAuthManifestListsExpectedTemplateFiles()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");

        Assert.Equal(ExpectedTemplateFiles, package.Manifest.Files);
    }

    [Fact]
    public void BasicAuthAppWiresSplashLoadingLoginAndHomeFlow()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        var appPath = Path.Combine(package.RootDirectory, "App.tsx");

        var content = File.ReadAllText(appPath);

        Assert.Contains("AuthProvider", content);
        Assert.Contains("useAuthSession", content);
        Assert.Contains("SplashScreen", content);
        Assert.Contains("LoadingScreen", content);
        Assert.Contains("LoginScreen", content);
        Assert.Contains("HomeScreen", content);
        Assert.Contains("state.status === 'checking'", content);
        Assert.Contains("state.status === 'signingIn'", content);
        Assert.Contains("state.status === 'signedIn'", content);
    }

    [Fact]
    public void BasicAuthProviderKeepsAuthStateMinimalAndReplaceable()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        var providerPath = Path.Combine(package.RootDirectory, "src/auth/AuthProvider.tsx");

        var content = File.ReadAllText(providerPath);

        Assert.Contains("export type AuthState", content);
        Assert.Contains("status: 'checking'", content);
        Assert.Contains("status: 'signedOut'", content);
        Assert.Contains("status: 'signingIn'", content);
        Assert.Contains("status: 'signedIn'; user: AuthUser", content);
        Assert.Contains("signIn: (email: string, password: string) => void", content);
        Assert.Contains("signOut: () => void", content);
        Assert.DoesNotContain("fetch(", content);
        Assert.DoesNotContain("axios", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BasicAuthIncludesSafeExampleConfigurationFiles()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");

        var envExample = File.ReadAllText(Path.Combine(package.RootDirectory, ".env.example"));
        var credentialsExample = File.ReadAllText(Path.Combine(package.RootDirectory, "credentials.example.json"));

        Assert.Contains("APP_ENV=development", envExample);
        Assert.Contains("API_BASE_URL=https://api.example.com", envExample);
        Assert.Contains("\"clientId\": \"replace-with-client-id\"", credentialsExample);
        Assert.Contains("\"tenantId\": \"replace-with-tenant-id\"", credentialsExample);
        Assert.Contains("\"redirectScheme\": \"replace-with-app-scheme\"", credentialsExample);
    }

    [Fact]
    public void BasicAuthDocumentsRealConfigurationFilesAsIgnored()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        var configuration = File.ReadAllText(Path.Combine(package.RootDirectory, "CONFIGURATION.md"));

        Assert.Contains(".env", configuration);
        Assert.Contains(".env.*", configuration);
        Assert.Contains("credentials.json", configuration);
        Assert.Contains("credentials.*.json", configuration);
        Assert.Contains("Keep real local configuration files out of Git", configuration);
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

    [Fact]
    public void BasicAuthTemplateFilesDoNotIncludeSecretLikeValues()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");

        foreach (var relativePath in package.Manifest.Files)
        {
            var content = File.ReadAllText(Path.Combine(package.RootDirectory, relativePath));

            Assert.DoesNotContain("client_secret", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api_key", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BEGIN PRIVATE KEY", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("access_token =", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
