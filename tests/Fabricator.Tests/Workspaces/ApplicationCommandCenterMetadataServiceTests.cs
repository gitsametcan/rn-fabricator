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
              "projectIntelligence": {
                "targetUsers": 10000,
                "targetDate": "2026-12-31",
                "reportingCadence": "weekly",
                "metricSnapshots": [
                  {
                    "date": "2026-07-01",
                    "acquiredUsers": 1200,
                    "activeUsers": 860,
                    "retentionProxy": 0.72,
                    "notes": "Beta launch baseline."
                  },
                  {
                    "date": "2026-07-15",
                    "acquiredUsers": 1650,
                    "activeUsers": 1200
                  }
                ],
                "milestoneProgress": [
                  {
                    "id": "beta",
                    "title": "Beta release",
                    "status": "ready",
                    "progressPercent": 100,
                    "notes": "Closed beta is live."
                  }
                ],
                "assumptions": ["Weekly acquisition stays above 200 users."],
                "notes": "Track whether launch target is realistic."
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
        Assert.Equal(10000, result.Metadata.ProjectIntelligence.TargetUsers);
        Assert.Equal(new DateOnly(2026, 12, 31), result.Metadata.ProjectIntelligence.TargetDate);
        Assert.Equal("weekly", result.Metadata.ProjectIntelligence.ReportingCadence);
        Assert.Equal(2, result.Metadata.ProjectIntelligence.MetricSnapshots.Count);
        Assert.Equal(1200, result.Metadata.ProjectIntelligence.MetricSnapshots[0].AcquiredUsers);
        Assert.Equal(860, result.Metadata.ProjectIntelligence.MetricSnapshots[0].ActiveUsers);
        Assert.Equal(0.72m, result.Metadata.ProjectIntelligence.MetricSnapshots[0].RetentionProxy);
        Assert.Equal("beta", Assert.Single(result.Metadata.ProjectIntelligence.MilestoneProgress).Id);
        Assert.Equal(["Weekly acquisition stays above 200 users."], result.Metadata.ProjectIntelligence.Assumptions);
        Assert.Equal("privacy-policy", Assert.Single(result.Metadata.ReleaseChecklist.Items).Id);
        Assert.Equal("store-assets", Assert.Single(result.Metadata.NextActions).Id);
    }

    [Fact]
    public void ReadDefaultsMissingProjectIntelligenceMetadata()
    {
        using var project = new TemporaryDirectory();
        WriteCommandCenterMetadata(
            project.Path,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-app-command-center"
            }
            """);
        var service = new ApplicationCommandCenterMetadataService();

        var result = service.Read(project.Path);

        Assert.True(result.HasMetadata);
        Assert.Null(result.Metadata.ProjectIntelligence.TargetUsers);
        Assert.Null(result.Metadata.ProjectIntelligence.TargetDate);
        Assert.Null(result.Metadata.ProjectIntelligence.ReportingCadence);
        Assert.Empty(result.Metadata.ProjectIntelligence.MetricSnapshots);
        Assert.Empty(result.Metadata.ProjectIntelligence.MilestoneProgress);
        Assert.Empty(result.Metadata.ProjectIntelligence.Assumptions);
        Assert.Null(result.Metadata.ProjectIntelligence.Notes);
    }

    [Fact]
    public void ReadLoadsEmptyProjectIntelligenceMetadata()
    {
        using var project = new TemporaryDirectory();
        WriteCommandCenterMetadata(
            project.Path,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-app-command-center",
              "projectIntelligence": {}
            }
            """);
        var service = new ApplicationCommandCenterMetadataService();

        var result = service.Read(project.Path);

        Assert.True(result.HasMetadata);
        Assert.Empty(result.Metadata.ProjectIntelligence.MetricSnapshots);
        Assert.Empty(result.Metadata.ProjectIntelligence.MilestoneProgress);
        Assert.Empty(result.Metadata.ProjectIntelligence.Assumptions);
    }

    [Fact]
    public void ReadLoadsPartiallySpecifiedProjectIntelligenceMetadata()
    {
        using var project = new TemporaryDirectory();
        WriteCommandCenterMetadata(
            project.Path,
            """
            {
              "schemaVersion": 1,
              "kind": "fabricator-app-command-center",
              "projectIntelligence": {
                "targetUsers": 5000,
                "metricSnapshots": [
                  {
                    "date": "2026-07-19",
                    "acquiredUsers": 900,
                    "activeUsers": 450
                  }
                ]
              }
            }
            """);
        var service = new ApplicationCommandCenterMetadataService();

        var result = service.Read(project.Path);

        Assert.True(result.HasMetadata);
        Assert.Equal(5000, result.Metadata.ProjectIntelligence.TargetUsers);
        Assert.Null(result.Metadata.ProjectIntelligence.TargetDate);
        Assert.Equal(new DateOnly(2026, 7, 19), Assert.Single(result.Metadata.ProjectIntelligence.MetricSnapshots).Date);
        Assert.Empty(result.Metadata.ProjectIntelligence.Assumptions);
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
