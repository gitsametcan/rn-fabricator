using Fabricator.Core.Projects;
using Fabricator.Core.Workspaces;

namespace Fabricator.Tests.Workspaces;

public sealed class ApplicationCommandCenterMetadataServiceTests
{
    [Fact]
    public void ReadLoadsApplicationCommandCenterMetadata()
    {
        using var project = new TemporaryDirectory();
        WriteCommandCenterMetadata(
            project.Path,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-app-command-center",
              "publishing": {
                "ios": {
                  "displayName": "Demo iOS",
                  "identifier": "com.example.ios",
                  "version": "1.2.3",
                  "buildNumber": "45",
                  "status": "ready"
                },
                "android": {
                  "displayName": "Demo Android",
                  "identifier": "com.example.android",
                  "version": "1.2.3",
                  "buildNumber": "46",
                  "status": "missing"
                },
                "releaseOwner": "Mobile Team",
                "notes": "Prepare store assets."
              },
              "marketResearch": {
                "targetAudience": "Solo founders",
                "positioning": "Fast launch kit",
                "keywords": ["launch", "mobile"],
                "competitors": ["Competitor A"],
                "openQuestions": ["Which market comes first?"],
                "notes": "Validate demand."
              },
              "releaseChecklist": {
                "items": [
                  {
                    "id": "privacy-policy",
                    "title": "Privacy policy",
                    "status": "ready",
                    "notes": "Published."
                  }
                ],
                "notes": "Release checklist notes."
              },
              "nextActions": [
                {
                  "id": "store-assets",
                  "title": "Prepare store screenshots",
                  "group": "publishing",
                  "status": "open",
                  "notes": "Use real device frames."
                }
              ]
            }
            """);
        var service = new ApplicationCommandCenterMetadataService();

        var result = service.Read(project.Path);

        Assert.True(result.Exists);
        Assert.True(result.IsValid);
        Assert.True(result.HasMetadata);
        Assert.Empty(result.Errors);
        Assert.EndsWith(ApplicationCommandCenterMetadataService.MetadataRelativePath, result.MetadataPath);
        Assert.Equal("Demo iOS", result.Metadata.Publishing.Ios.DisplayName);
        Assert.Equal("com.example.android", result.Metadata.Publishing.Android.Identifier);
        Assert.Equal("Mobile Team", result.Metadata.Publishing.ReleaseOwner);
        Assert.Equal("Solo founders", result.Metadata.MarketResearch.TargetAudience);
        Assert.Equal(["launch", "mobile"], result.Metadata.MarketResearch.Keywords);
        Assert.Equal("privacy-policy", Assert.Single(result.Metadata.ReleaseChecklist.Items).Id);
        Assert.Equal("store-assets", Assert.Single(result.Metadata.NextActions).Id);
    }

    [Fact]
    public void ReadReturnsExplicitMissingStateWhenMetadataFileDoesNotExist()
    {
        using var project = new TemporaryDirectory();
        var service = new ApplicationCommandCenterMetadataService();

        var result = service.Read(project.Path);

        Assert.False(result.Exists);
        Assert.True(result.IsValid);
        Assert.False(result.HasMetadata);
        Assert.Empty(result.Errors);
        Assert.Equal(ApplicationCommandCenterMetadata.Empty, result.Metadata);
    }

    [Fact]
    public void ReadReturnsInvalidStateForMalformedMetadata()
    {
        using var project = new TemporaryDirectory();
        WriteCommandCenterMetadata(project.Path, "{ not-json");
        var service = new ApplicationCommandCenterMetadataService();

        var result = service.Read(project.Path);

        Assert.True(result.Exists);
        Assert.False(result.IsValid);
        Assert.False(result.HasMetadata);
        Assert.Contains(result.Errors, error => error.Contains("could not be read", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadReturnsInvalidStateForUnsupportedContract()
    {
        using var project = new TemporaryDirectory();
        WriteCommandCenterMetadata(
            project.Path,
            """
            {
              "schemaVersion": 99,
              "kind": "other-kind"
            }
            """);
        var service = new ApplicationCommandCenterMetadataService();

        var result = service.Read(project.Path);

        Assert.True(result.Exists);
        Assert.False(result.IsValid);
        Assert.False(result.HasMetadata);
        Assert.Contains(result.Errors, error => error.Contains("schema version 99 is not supported", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("kind must be fabricator-app-command-center", StringComparison.Ordinal));
    }

    [Fact]
    public void WorkspaceProjectDetailIncludesCommandCenterMetadataWithoutChangingFabricatorState()
    {
        using var project = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(project.Path, ".fabricator"));
        File.WriteAllText(Path.Combine(project.Path, FabricatorProjectContract.ManifestRelativePath), "{}");
        File.WriteAllText(
            Path.Combine(project.Path, FabricatorProjectStateContract.StateRelativePath),
            """
            {
              "project": {
                "name": "CommandCenterDemo"
              },
              "appliedTemplates": [
                { "id": "minimal-splash" }
              ]
            }
            """);
        WriteCommandCenterMetadata(
            project.Path,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-app-command-center",
              "marketResearch": {
                "positioning": "Local-first planning."
              }
            }
            """);
        var service = new WorkspaceProjectDetailService();

        var detail = service.GetDetail(project.Path);

        Assert.Equal("CommandCenterDemo", detail.DisplayName);
        Assert.Equal(1, detail.AppliedTemplateCount);
        Assert.True(detail.CommandCenterMetadata.HasMetadata);
        Assert.Equal("Local-first planning.", detail.CommandCenterMetadata.Metadata.MarketResearch.Positioning);
    }

    private static void WriteCommandCenterMetadata(string projectPath, string contents)
    {
        var metadataPath = Path.Combine(
            projectPath,
            ApplicationCommandCenterMetadataService.MetadataRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
        File.WriteAllText(metadataPath, contents);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"fabricator-command-center-tests-{Guid.NewGuid():N}");
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
