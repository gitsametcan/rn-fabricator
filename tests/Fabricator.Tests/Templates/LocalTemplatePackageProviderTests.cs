using Fabricator.Core.Templates;

namespace Fabricator.Tests.Templates;

public sealed class LocalTemplatePackageProviderTests
{
    [Fact]
    public void GetTemplateLoadsBasicAuthManifestFromBuildOutput()
    {
        var provider = new LocalTemplatePackageProvider();

        var package = provider.GetTemplate("basic-auth");

        Assert.Equal("basic-auth", package.Manifest.Id);
        Assert.Equal("Basic Auth", package.Manifest.DisplayName);
        Assert.Equal("0.1.0", package.Manifest.Version);
        Assert.EndsWith(Path.Combine("Templates", "basic-auth"), package.RootDirectory);
        Assert.True(File.Exists(Path.Combine(package.RootDirectory, LocalTemplatePackageProvider.ManifestFileName)));
    }

    [Fact]
    public void GetTemplateLoadsManifestFromCustomTemplatesRoot()
    {
        using var templatesRoot = new TemporaryDirectory();
        var templateDirectory = Path.Combine(templatesRoot.Path, "custom-template");
        Directory.CreateDirectory(templateDirectory);
        File.WriteAllText(
            Path.Combine(templateDirectory, LocalTemplatePackageProvider.ManifestFileName),
            """
            {
              "id": "custom-template",
              "displayName": "Custom Template",
              "description": "A custom template for tests.",
              "version": "1.2.3",
              "files": [
                "src/App.tsx"
              ]
            }
            """);
        var provider = new LocalTemplatePackageProvider(templatesRoot.Path);

        var package = provider.GetTemplate("custom-template");

        Assert.Equal("custom-template", package.Manifest.Id);
        Assert.Equal("Custom Template", package.Manifest.DisplayName);
        Assert.Equal(["src/App.tsx"], package.Manifest.Files);
    }

    [Theory]
    [InlineData("../basic-auth")]
    [InlineData("basic-auth/nested")]
    [InlineData("basic-auth\\nested")]
    public void GetTemplateRejectsTemplateIdsWithPathSeparators(string templateId)
    {
        using var templatesRoot = new TemporaryDirectory();
        var provider = new LocalTemplatePackageProvider(templatesRoot.Path);

        var exception = Assert.Throws<TemplatePackageException>(() => provider.GetTemplate(templateId));

        Assert.Contains("must not contain path separators", exception.Message);
    }

    [Fact]
    public void GetTemplateThrowsWhenManifestDoesNotExist()
    {
        using var templatesRoot = new TemporaryDirectory();
        var provider = new LocalTemplatePackageProvider(templatesRoot.Path);

        var exception = Assert.Throws<TemplatePackageException>(() => provider.GetTemplate("missing"));

        Assert.Contains("Template manifest was not found", exception.Message);
    }

    [Fact]
    public void GetTemplateThrowsWhenManifestIdDoesNotMatchDirectory()
    {
        using var templatesRoot = new TemporaryDirectory();
        var templateDirectory = Path.Combine(templatesRoot.Path, "requested");
        Directory.CreateDirectory(templateDirectory);
        File.WriteAllText(
            Path.Combine(templateDirectory, LocalTemplatePackageProvider.ManifestFileName),
            """
            {
              "id": "different",
              "displayName": "Different",
              "description": "Different template.",
              "version": "1.0.0",
              "files": []
            }
            """);
        var provider = new LocalTemplatePackageProvider(templatesRoot.Path);

        var exception = Assert.Throws<TemplatePackageException>(() => provider.GetTemplate("requested"));

        Assert.Contains("does not match requested template", exception.Message);
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
