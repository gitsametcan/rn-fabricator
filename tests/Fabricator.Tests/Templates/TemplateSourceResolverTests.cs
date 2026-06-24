using Fabricator.Core.Templates;

namespace Fabricator.Tests.Templates;

public sealed class TemplateSourceResolverTests
{
    [Fact]
    public void ResolveUsesExplicitSourceFirst()
    {
        using var directory = new TemporaryDirectory();
        var environment = new FakeEnvironmentVariables();
        environment.Set(TemplateSourceResolver.EnvironmentVariableName, "env/catalog.fabricator.json");
        var resolver = new TemplateSourceResolver(environment);

        var result = resolver.Resolve(new TemplateSourceResolutionRequest(
            "explicit/catalog.fabricator.json",
            directory.Path));

        Assert.True(result.Succeeded);
        Assert.Equal("explicit/catalog.fabricator.json", result.Source);
        Assert.Equal("explicit --source", result.SourceDescription);
    }

    [Fact]
    public void ResolveUsesEnvironmentSourceBeforeProjectState()
    {
        using var directory = new TemporaryDirectory();
        WriteFabricatorState(directory.Path, "./state/catalog.fabricator.json");
        var environment = new FakeEnvironmentVariables();
        environment.Set(TemplateSourceResolver.EnvironmentVariableName, "env/catalog.fabricator.json");
        var resolver = new TemplateSourceResolver(environment);

        var result = resolver.Resolve(new TemplateSourceResolutionRequest(null, directory.Path));

        Assert.True(result.Succeeded);
        Assert.Equal(Path.Combine(directory.Path, "env", "catalog.fabricator.json"), result.Source);
        Assert.Equal(TemplateSourceResolver.EnvironmentVariableName, result.SourceDescription);
    }

    [Fact]
    public void ResolveUsesDefaultLocalSourceFromFabricatorState()
    {
        using var directory = new TemporaryDirectory();
        WriteFabricatorState(directory.Path, "./templates/catalog.fabricator.json");
        var resolver = new TemplateSourceResolver(new FakeEnvironmentVariables());

        var result = resolver.Resolve(new TemplateSourceResolutionRequest(null, directory.Path));

        Assert.True(result.Succeeded);
        Assert.Equal(Path.Combine(directory.Path, "templates", "catalog.fabricator.json"), result.Source);
        Assert.Equal("fabricator.json:local", result.SourceDescription);
    }

    [Fact]
    public void ResolveSkipsEmbeddedStateSourceAndUsesConventionalCatalog()
    {
        using var directory = new TemporaryDirectory();
        WriteEmbeddedFabricatorState(directory.Path);
        var conventionalCatalog = Path.Combine(directory.Path, TemplateSourceResolver.ConventionalCatalogRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(conventionalCatalog)!);
        File.WriteAllText(conventionalCatalog, "{}");
        var resolver = new TemplateSourceResolver(new FakeEnvironmentVariables());

        var result = resolver.Resolve(new TemplateSourceResolutionRequest(null, directory.Path));

        Assert.True(result.Succeeded);
        Assert.Equal(conventionalCatalog, result.Source);
        Assert.Equal(TemplateSourceResolver.ConventionalCatalogRelativePath, result.SourceDescription);
    }

    [Fact]
    public void ResolveUsesProjectStateBeforeWorkingDirectoryState()
    {
        using var workingDirectory = new TemporaryDirectory();
        using var projectDirectory = new TemporaryDirectory();
        WriteFabricatorState(workingDirectory.Path, "./working/catalog.fabricator.json");
        WriteFabricatorState(projectDirectory.Path, "./project/catalog.fabricator.json");
        var resolver = new TemplateSourceResolver(new FakeEnvironmentVariables());

        var result = resolver.Resolve(new TemplateSourceResolutionRequest(
            null,
            workingDirectory.Path,
            projectDirectory.Path));

        Assert.True(result.Succeeded);
        Assert.Equal(Path.Combine(projectDirectory.Path, "project", "catalog.fabricator.json"), result.Source);
        Assert.Equal("fabricator.json:local", result.SourceDescription);
    }

    [Fact]
    public void ResolveFailsWhenNoSourceCanBeFound()
    {
        using var directory = new TemporaryDirectory();
        var resolver = new TemplateSourceResolver(new FakeEnvironmentVariables());

        var result = resolver.Resolve(new TemplateSourceResolutionRequest(null, directory.Path));

        Assert.False(result.Succeeded);
        Assert.Contains("--source", result.ErrorMessage);
        Assert.Contains(TemplateSourceResolver.EnvironmentVariableName, result.ErrorMessage);
        Assert.Contains(TemplateSourceResolver.ConventionalCatalogRelativePath, result.ErrorMessage);
    }

    private static void WriteFabricatorState(string directory, string sourceValue)
    {
        File.WriteAllText(
            Path.Combine(directory, "fabricator.json"),
            $$"""
            {
              "schemaVersion": 1,
              "kind": "fabricator-project-state",
              "templateSources": [
                {
                  "name": "local",
                  "type": "local",
                  "value": "{{sourceValue.Replace("\\", "\\\\", StringComparison.Ordinal)}}",
                  "isDefault": true
                }
              ],
              "appliedTemplates": []
            }
            """);
    }

    private static void WriteEmbeddedFabricatorState(string directory)
    {
        File.WriteAllText(
            Path.Combine(directory, "fabricator.json"),
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-project-state",
              "templateSources": [
                {
                  "name": "embedded",
                  "type": "embedded",
                  "value": "minimal-splash",
                  "isDefault": true
                }
              ],
              "appliedTemplates": []
            }
            """);
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
