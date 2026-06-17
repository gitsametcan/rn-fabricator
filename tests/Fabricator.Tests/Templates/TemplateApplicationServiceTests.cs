using Fabricator.Core.Templates;

namespace Fabricator.Tests.Templates;

public sealed class TemplateApplicationServiceTests
{
    [Fact]
    public void ApplyGeneratesExpectedBasicAuthFiles()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        using var targetDirectory = new TemporaryDirectory();
        var service = new TemplateApplicationService();

        var result = service.Apply(new TemplateApplicationRequest(package, targetDirectory.Path));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Equal(package.Manifest.Files, result.GeneratedFiles);

        foreach (var relativePath in package.Manifest.Files)
        {
            Assert.True(
                File.Exists(Path.Combine(targetDirectory.Path, relativePath)),
                $"Expected generated file was missing: {relativePath}");
        }
    }

    [Fact]
    public void ApplyDoesNotSilentlyOverwriteExistingFiles()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        using var targetDirectory = new TemporaryDirectory();
        var existingAppPath = Path.Combine(targetDirectory.Path, "App.tsx");
        File.WriteAllText(existingAppPath, "existing app");
        var service = new TemplateApplicationService();

        var result = service.Apply(new TemplateApplicationRequest(package, targetDirectory.Path));

        Assert.False(result.Succeeded);
        Assert.Contains("App.tsx", result.SkippedFiles);
        Assert.Contains(result.Errors, error => error.Contains("Target file already exists", StringComparison.Ordinal));
        Assert.Equal("existing app", File.ReadAllText(existingAppPath));
    }

    [Fact]
    public void ApplyCanOverwriteExistingFilesWhenExplicitlyRequested()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        using var targetDirectory = new TemporaryDirectory();
        var existingAppPath = Path.Combine(targetDirectory.Path, "App.tsx");
        File.WriteAllText(existingAppPath, "existing app");
        var service = new TemplateApplicationService();

        var result = service.Apply(new TemplateApplicationRequest(
            package,
            targetDirectory.Path,
            OverwriteExistingFiles: true));

        Assert.True(result.Succeeded);
        Assert.Empty(result.SkippedFiles);
        Assert.Contains("AuthProvider", File.ReadAllText(existingAppPath));
    }

    [Fact]
    public void ApplyKeepsExampleFilesAsPlaceholdersOnly()
    {
        var package = new LocalTemplatePackageProvider().GetTemplate("basic-auth");
        using var targetDirectory = new TemporaryDirectory();
        var service = new TemplateApplicationService();

        var result = service.Apply(new TemplateApplicationRequest(package, targetDirectory.Path));

        Assert.True(result.Succeeded);

        var envExample = File.ReadAllText(Path.Combine(targetDirectory.Path, ".env.example"));
        var credentialsExample = File.ReadAllText(Path.Combine(targetDirectory.Path, "credentials.example.json"));

        Assert.Contains("https://api.example.com", envExample);
        Assert.Contains("replace-with-client-id", credentialsExample);
        Assert.Contains("replace-with-tenant-id", credentialsExample);
        Assert.DoesNotContain("client_secret", credentialsExample, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api_key", credentialsExample, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", credentialsExample, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyRejectsManifestEntriesThatResolveOutsideTargetDirectory()
    {
        using var packageRoot = new TemporaryDirectory();
        using var targetDirectory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(packageRoot.Path, "safe.txt"), "safe");
        var package = new TemplatePackage(
            new TemplateManifest(
                "unsafe",
                "Unsafe",
                "Unsafe test package.",
                "1.0.0",
                ["../outside.txt", "safe.txt"]),
            packageRoot.Path);
        var service = new TemplateApplicationService();

        var result = service.Apply(new TemplateApplicationRequest(package, targetDirectory.Path));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("outside the package root", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(targetDirectory.Path, "..", "outside.txt")));
        Assert.True(File.Exists(Path.Combine(targetDirectory.Path, "safe.txt")));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rn-fabricator-tests-{Guid.NewGuid():N}");
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
