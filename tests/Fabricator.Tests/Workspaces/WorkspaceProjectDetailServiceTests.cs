using Fabricator.Core.Projects;
using Fabricator.Core.Workspaces;

namespace Fabricator.Tests.Workspaces;

public sealed class WorkspaceProjectDetailServiceTests
{
    [Fact]
    public void GetDetailReadsFabricatorIdentityAndTemplateCount()
    {
        using var project = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(project.Path, ".fabricator"));
        File.WriteAllText(Path.Combine(project.Path, FabricatorProjectContract.ManifestRelativePath), "{}");
        File.WriteAllText(
            Path.Combine(project.Path, FabricatorProjectStateContract.StateRelativePath),
            """
            {
              "project": {
                "name": "FabricatorDemo"
              },
              "appliedTemplates": [
                { "id": "minimal-splash" },
                { "id": "component/primary-button" }
              ]
            }
            """);
        File.WriteAllText(
            Path.Combine(project.Path, "package.json"),
            """
            {
              "name": "fabricator-demo",
              "version": "2.3.4"
            }
            """);
        var service = new WorkspaceProjectDetailService();

        var detail = service.GetDetail(project.Path);

        Assert.True(detail.ProjectExists);
        Assert.Equal("FabricatorDemo", detail.DisplayName);
        Assert.Equal("fabricator-demo", detail.PackageName);
        Assert.True(detail.HasFabricatorManifest);
        Assert.True(detail.HasFabricatorState);
        Assert.Equal(2, detail.AppliedTemplateCount);
        Assert.Equal("2.3.4", detail.StoreMetadata.AppVersion);
    }

    [Fact]
    public void GetDetailFallsBackToAppJsonAndFolderName()
    {
        using var appJsonProject = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(appJsonProject.Path, "app.json"),
            """
            {
              "displayName": "App Json Name",
              "name": "InternalName"
            }
            """);
        using var emptyProject = new TemporaryDirectory();
        var service = new WorkspaceProjectDetailService();

        var appJsonDetail = service.GetDetail(appJsonProject.Path);
        var emptyDetail = service.GetDetail(emptyProject.Path);

        Assert.Equal("App Json Name", appJsonDetail.DisplayName);
        Assert.Equal(Path.GetFileName(emptyProject.Path), emptyDetail.DisplayName);
    }

    [Fact]
    public void GetDetailCountsProjectStatistics()
    {
        using var project = new TemporaryDirectory();
        WriteFile(project.Path, "src/screens/HomeScreen.tsx");
        WriteFile(project.Path, "src/components/PrimaryButton.tsx");
        WriteFile(project.Path, "src/services/apiClient.ts");
        WriteFile(project.Path, "src/utils/storage.ts");
        WriteFile(project.Path, "src/navigation/AppNavigator.tsx");
        var service = new WorkspaceProjectDetailService();

        var detail = service.GetDetail(project.Path);

        Assert.Equal(5, detail.Statistics.SourceFileCount);
        Assert.Equal(1, detail.Statistics.ScreenFileCount);
        Assert.Equal(1, detail.Statistics.ComponentFileCount);
        Assert.Equal(1, detail.Statistics.ServiceFileCount);
        Assert.Equal(1, detail.Statistics.UtilityFileCount);
    }

    [Fact]
    public void GetDetailReadsBestEffortStoreMetadata()
    {
        using var project = new TemporaryDirectory();
        WriteFile(
            project.Path,
            "ios/Demo/Info.plist",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <plist version="1.0">
              <dict>
                <key>CFBundleDisplayName</key>
                <string>Demo iOS</string>
                <key>CFBundleIdentifier</key>
                <string>com.example.iosdemo</string>
                <key>CFBundleShortVersionString</key>
                <string>1.2.3</string>
                <key>CFBundleVersion</key>
                <string>45</string>
              </dict>
            </plist>
            """);
        WriteFile(
            project.Path,
            "android/app/src/main/AndroidManifest.xml",
            """
            <manifest xmlns:android="http://schemas.android.com/apk/res/android" package="com.example.manifest">
              <application android:label="Demo Android" />
            </manifest>
            """);
        WriteFile(
            project.Path,
            "android/app/build.gradle",
            """
            android {
              defaultConfig {
                applicationId "com.example.androiddemo"
                versionName "4.5.6"
                versionCode 78
              }
            }
            """);
        var service = new WorkspaceProjectDetailService();

        var detail = service.GetDetail(project.Path);

        Assert.Equal("Demo iOS", detail.StoreMetadata.AppStoreName);
        Assert.Equal("Demo Android", detail.StoreMetadata.PlayStoreName);
        Assert.Equal("com.example.iosdemo", detail.StoreMetadata.IosBundleIdentifier);
        Assert.Equal("com.example.androiddemo", detail.StoreMetadata.AndroidApplicationId);
        Assert.Equal("4.5.6", detail.StoreMetadata.AppVersion);
        Assert.Equal("78", detail.StoreMetadata.BuildNumber);
    }

    [Fact]
    public void GetDetailReturnsMissingMetadataAsNullValues()
    {
        using var project = new TemporaryDirectory();
        var service = new WorkspaceProjectDetailService();

        var detail = service.GetDetail(project.Path);

        Assert.Null(detail.PackageName);
        Assert.Null(detail.StoreMetadata.AppStoreName);
        Assert.Null(detail.StoreMetadata.PlayStoreName);
        Assert.Null(detail.StoreMetadata.IosBundleIdentifier);
        Assert.Null(detail.StoreMetadata.AndroidApplicationId);
        Assert.Null(detail.StoreMetadata.AppVersion);
        Assert.Null(detail.StoreMetadata.BuildNumber);
    }

    private static void WriteFile(string root, string relativePath, string contents = "export {};\n")
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"fabricator-project-detail-tests-{Guid.NewGuid():N}");
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
