using System.Text.Json;
using System.Text.Json.Serialization;
using Fabricator.Core.Toolchains;

namespace Fabricator.Tests.Toolchains;

public sealed class LocalToolchainProfileProviderTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    [Fact]
    public void DefaultProviderLoadsPackagedProfiles()
    {
        var provider = new LocalToolchainProfileProvider();

        var result = provider.GetDefaultProfile();

        Assert.True(result.Succeeded);
        Assert.Equal("react-native-stable", result.Profile?.Id);
    }

    [Fact]
    public void GetDefaultProfileReturnsSingleDefaultProfile()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(isDefault: true));
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var result = provider.GetDefaultProfile();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Profile);
        Assert.Equal("react-native-stable", result.Profile.Id);
        Assert.Equal("0.76.x", result.Profile.ReactNativeVersion);
    }

    [Fact]
    public void GetProfileFindsProfileById()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-0.76", reactNativeVersion: "0.76.x"));
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var result = provider.GetProfile("react-native-0.76");

        Assert.True(result.Succeeded);
        Assert.Equal("react-native-0.76", result.Profile?.Id);
    }

    [Fact]
    public void GetProfileFindsProfileByReactNativeVersion()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-0.76", reactNativeVersion: "0.76.x"));
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var result = provider.GetProfile("0.76.x");

        Assert.True(result.Succeeded);
        Assert.Equal("react-native-0.76", result.Profile?.Id);
    }

    [Fact]
    public void GetProfileReturnsClearErrorForUnsupportedProfile()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile());
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var result = provider.GetProfile("0.99.x");

        Assert.False(result.Succeeded);
        Assert.Null(result.Profile);
        Assert.Contains("Unsupported React Native toolchain profile: 0.99.x.", result.Errors);
    }

    [Fact]
    public void GetDefaultProfileReturnsErrorWhenNoDefaultExists()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(isDefault: false));
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var result = provider.GetDefaultProfile();

        Assert.False(result.Succeeded);
        Assert.Contains("No default toolchain profile was found.", result.Errors);
    }

    [Fact]
    public void GetDefaultProfileReturnsErrorWhenMultipleDefaultsExist()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-stable", reactNativeVersion: "0.76.x", isDefault: true), "stable.json");
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-next", reactNativeVersion: "0.77.x", isDefault: true), "next.json");
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var result = provider.GetDefaultProfile();

        Assert.False(result.Succeeded);
        Assert.Contains("Multiple default toolchain profiles were found.", result.Errors);
    }

    [Fact]
    public void GetProfilesThrowsWhenProfileDirectoryIsMissing()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"rn-fabricator-missing-{Guid.NewGuid():N}");
        var provider = new LocalToolchainProfileProvider(missingDirectory);

        var exception = Assert.Throws<ToolchainProfileProviderException>(provider.GetProfiles);

        Assert.Contains("Toolchain profile directory was not found:", exception.Message);
    }

    [Fact]
    public void GetProfilesThrowsWhenProfileIsInvalid()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(id: ""));
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var exception = Assert.Throws<ToolchainProfileProviderException>(provider.GetProfiles);

        Assert.Contains("Toolchain profile is invalid:", exception.Message);
        Assert.Contains("Profile id is required.", exception.Message);
    }

    [Fact]
    public void GetProfilesThrowsWhenProfileIdsAreDuplicated()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-stable", reactNativeVersion: "0.76.x"), "stable.json");
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-stable", reactNativeVersion: "0.77.x"), "duplicate.json");
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var exception = Assert.Throws<ToolchainProfileProviderException>(provider.GetProfiles);

        Assert.Contains("Duplicate toolchain profile id: react-native-stable", exception.Message);
    }

    [Fact]
    public void GetProfilesThrowsWhenReactNativeVersionsAreDuplicated()
    {
        using var profileDirectory = new TemporaryDirectory();
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-stable", reactNativeVersion: "0.76.x"), "stable.json");
        WriteProfile(profileDirectory.Path, CreateProfile(id: "react-native-next", reactNativeVersion: "0.76.x"), "duplicate.json");
        var provider = new LocalToolchainProfileProvider(profileDirectory.Path);

        var exception = Assert.Throws<ToolchainProfileProviderException>(provider.GetProfiles);

        Assert.Contains("Duplicate React Native toolchain version: 0.76.x", exception.Message);
    }

    private static void WriteProfile(
        string directory,
        ToolchainProfile profile,
        string fileName = "react-native-stable.json")
    {
        File.WriteAllText(
            Path.Combine(directory, fileName),
            JsonSerializer.Serialize(profile, SerializerOptions));
    }

    private static ToolchainProfile CreateProfile(
        string id = "react-native-stable",
        string reactNativeVersion = "0.76.x",
        bool isDefault = true)
    {
        return new ToolchainProfile(
            id,
            "React Native Stable",
            reactNativeVersion,
            isDefault,
            NodeJs: new ToolchainRequirement(
                "Node.js",
                ToolchainRecommendationStrategy.Lts),
            Java: new ToolchainRequirement(
                "Java",
                ToolchainRecommendationStrategy.SupportedRange,
                SupportedVersionRange: "17-21"),
            Xcode: new ToolchainRequirement(
                "Xcode",
                ToolchainRecommendationStrategy.MinimumVersion,
                MinimumVersion: "15.0",
                Required: false),
            CocoaPods: new ToolchainRequirement(
                "CocoaPods",
                ToolchainRecommendationStrategy.MinimumVersion,
                MinimumVersion: "1.14.0",
                Required: false),
            Watchman: new ToolchainRequirement(
                "Watchman",
                ToolchainRecommendationStrategy.LatestStable,
                Required: false),
            Android: new AndroidToolchainRequirement(
                CompileSdk: 35,
                TargetSdk: 35,
                MinSdk: 23,
                PlatformTools: new ToolchainRequirement(
                    "Android Platform Tools",
                    ToolchainRecommendationStrategy.LatestStable),
                RequiredPackages:
                [
                    "platforms;android-35",
                    "platform-tools"
                ]));
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
