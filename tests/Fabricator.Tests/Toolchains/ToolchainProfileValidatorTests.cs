using Fabricator.Core.Toolchains;

namespace Fabricator.Tests.Toolchains;

public sealed class ToolchainProfileValidatorTests
{
    private readonly ToolchainProfileValidator _validator = new();

    [Fact]
    public void ValidateAcceptsDefaultStableProfileShape()
    {
        var profile = CreateValidProfile(isDefault: true);

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateAcceptsVersionSpecificProfileShape()
    {
        var profile = CreateValidProfile(
            id: "react-native-0.76",
            displayName: "React Native 0.76",
            reactNativeVersion: "0.76.x",
            isDefault: false);

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(ToolchainRecommendationStrategy.ExactVersion, null, null, null, "Java exact version recommendation is required.")]
    [InlineData(ToolchainRecommendationStrategy.MinimumVersion, null, null, null, "Java minimum version is required.")]
    [InlineData(ToolchainRecommendationStrategy.SupportedRange, null, null, null, "Java supported version range is required.")]
    public void ValidateRejectsMissingStrategyVersionData(
        ToolchainRecommendationStrategy strategy,
        string? recommendedVersion,
        string? minimumVersion,
        string? supportedVersionRange,
        string expectedError)
    {
        var profile = CreateValidProfile() with
        {
            Java = new ToolchainRequirement(
                "Java",
                strategy,
                recommendedVersion,
                minimumVersion,
                supportedVersionRange)
        };

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors);
    }

    [Fact]
    public void ValidateRejectsInvalidProfileIdentity()
    {
        var profile = CreateValidProfile() with
        {
            Id = " ",
            DisplayName = "",
            ReactNativeVersion = "\t"
        };

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Profile id is required.", result.Errors);
        Assert.Contains("Profile display name is required.", result.Errors);
        Assert.Contains("React Native version is required.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsMissingProfile()
    {
        var result = _validator.Validate(null);

        Assert.False(result.IsValid);
        Assert.Contains("Toolchain profile is required.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsMissingRequirementSections()
    {
        var profile = CreateValidProfile() with
        {
            NodeJs = null!,
            Android = null!
        };

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Node.js requirement is required.", result.Errors);
        Assert.Contains("Android requirement is required.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsInvalidAndroidSdkValues()
    {
        var profile = CreateValidProfile() with
        {
            Android = new AndroidToolchainRequirement(
                CompileSdk: 0,
                TargetSdk: 23,
                MinSdk: 24,
                PlatformTools: new ToolchainRequirement(
                    "Android Platform Tools",
                    ToolchainRecommendationStrategy.LatestStable))
        };

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Android compile SDK must be greater than zero.", result.Errors);
        Assert.Contains("Android min SDK must not be greater than target SDK.", result.Errors);
    }

    private static ToolchainProfile CreateValidProfile(
        string id = "react-native-stable",
        string displayName = "React Native Stable",
        string reactNativeVersion = "0.76.x",
        bool isDefault = true)
    {
        return new ToolchainProfile(
            id,
            displayName,
            reactNativeVersion,
            isDefault,
            NodeJs: new ToolchainRequirement(
                "Node.js",
                ToolchainRecommendationStrategy.Lts,
                Notes: ["Use the active LTS release supported by React Native."]),
            Java: new ToolchainRequirement(
                "Java",
                ToolchainRecommendationStrategy.SupportedRange,
                SupportedVersionRange: "17-21"),
            Xcode: new ToolchainRequirement(
                "Xcode",
                ToolchainRecommendationStrategy.MinimumVersion,
                MinimumVersion: "15.0"),
            CocoaPods: new ToolchainRequirement(
                "CocoaPods",
                ToolchainRecommendationStrategy.MinimumVersion,
                MinimumVersion: "1.14.0"),
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
}
