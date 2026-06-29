using Fabricator.Core.Environment;
using Fabricator.Core.Setup;
using Fabricator.Core.Toolchains;

namespace Fabricator.Tests.Setup;

public sealed class SetupPlanServiceTests
{
    [Fact]
    public async Task BuildPlanAsyncCreatesCommandManualAndEnvironmentItems()
    {
        var dependencyCheckService = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Node.js", null, "Node.js missing.", "Install Node."),
                DependencyCheckResult.Failed("npm", null, "npm missing.", "Install npm.")
            ]),
            AppleToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Xcode", null, "Xcode missing.", "Install Xcode."),
                DependencyCheckResult.Failed("CocoaPods", null, "CocoaPods missing.", "Install CocoaPods."),
                DependencyCheckResult.Warning("Watchman", null, "Watchman missing.", "Install Watchman.")
            ]),
            AndroidToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Android SDK", null, "Android SDK missing.", "Install Android Studio.")
            ])
        };
        var service = new SetupPlanService(
            dependencyCheckService,
            new FakeSystemPlatform { IsMacOS = true },
            new FixedPackageManagerDetector(new PackageManagerInfo("Homebrew", true, "brew")),
            new FixedToolchainProfileProvider(CreateProfile()));

        var plan = await service.BuildPlanAsync(SetupPlanRequest.Default);

        Assert.Equal("macOS", plan.PlatformName);
        Assert.Equal("react-native-stable", plan.ToolchainProfile?.Id);
        Assert.Equal("0.76.x", plan.ToolchainProfile?.ReactNativeVersion);
        Assert.Equal("Homebrew", plan.PackageManager.Name);
        Assert.Contains(plan.Items, item =>
            item.DependencyName == "Node.js"
            && item.Kind == SetupPlanItemKind.Command
            && item.Steps.Contains("brew install node")
            && item.Steps.Contains("Profile recommendation for React Native 0.76.x: Node.js active LTS version."));
        Assert.DoesNotContain(plan.Items, item => item.DependencyName == "npm");
        Assert.Contains(plan.Items, item =>
            item.DependencyName == "Xcode"
            && item.Kind == SetupPlanItemKind.Manual);
        Assert.Contains(plan.Items, item =>
            item.DependencyName == "CocoaPods"
            && item.Kind == SetupPlanItemKind.Command
            && !item.RequiresAdmin
            && item.Steps.Contains("brew install cocoapods"));
        var androidSdkItem = plan.Items.Single(item => item.DependencyName == "Android SDK");
        Assert.Equal(SetupPlanItemKind.Environment, androidSdkItem.Kind);
        Assert.Contains(androidSdkItem.Steps, step => step.Contains("More Actions > SDK Manager", StringComparison.Ordinal));
        Assert.Contains(androidSdkItem.Steps, step => step.Contains("Android Studio > Settings > Languages & Frameworks > Android SDK", StringComparison.Ordinal));
        Assert.Contains("For the default macOS zsh shell, run `nano ~/.zshrc`.", androidSdkItem.Steps);
        Assert.Contains("Verify with `echo $ANDROID_HOME` and `adb --version`.", androidSdkItem.Steps);
        Assert.Equal(1, dependencyCheckService.CoreToolsCallCount);
        Assert.Equal(1, dependencyCheckService.AppleToolsCallCount);
        Assert.Equal(1, dependencyCheckService.AndroidToolsCallCount);
    }

    [Fact]
    public async Task BuildPlanAsyncFallsBackToElevatedCocoaPodsCommandWhenHomebrewIsMissing()
    {
        var dependencyCheckService = new FakeDependencyCheckService
        {
            AppleToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("CocoaPods", null, "CocoaPods missing.", "Install CocoaPods.")
            ])
        };
        var service = new SetupPlanService(
            dependencyCheckService,
            new FakeSystemPlatform { IsMacOS = true },
            new FixedPackageManagerDetector(PackageManagerInfo.NotDetected()),
            new FixedToolchainProfileProvider(CreateProfile()));

        var plan = await service.BuildPlanAsync(SetupPlanRequest.Default);

        var item = Assert.Single(plan.Items);
        Assert.Equal("CocoaPods", item.DependencyName);
        Assert.Equal(SetupPlanItemKind.Command, item.Kind);
        Assert.True(item.RequiresAdmin);
        Assert.Contains("sudo gem install cocoapods", item.Steps);
    }

    [Fact]
    public async Task BuildPlanAsyncUsesManualGuidanceWhenPackageManagerIsMissing()
    {
        var dependencyCheckService = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Git", null, "Git missing.", "Install Git.")
            ])
        };
        var service = new SetupPlanService(
            dependencyCheckService,
            new FakeSystemPlatform { IsWindows = true },
            new FixedPackageManagerDetector(PackageManagerInfo.NotDetected()),
            new FixedToolchainProfileProvider(CreateProfile()));

        var plan = await service.BuildPlanAsync(SetupPlanRequest.Default);

        var gitItem = Assert.Single(plan.Items);
        Assert.Equal("Windows", plan.PlatformName);
        Assert.Equal(SetupPlanItemKind.Manual, gitItem.Kind);
        Assert.Contains(gitItem.Steps, step => step.Contains("https://git-scm.com/downloads", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuildPlanAsyncSelectsExplicitReactNativeProfile()
    {
        var dependencyCheckService = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Java", null, "Java missing.", "Install Java.")
            ])
        };
        var provider = new FixedToolchainProfileProvider(
            CreateProfile(
                id: "react-native-0.77",
                reactNativeVersion: "0.77.x",
                javaRange: "17-21"));
        var service = new SetupPlanService(
            dependencyCheckService,
            new FakeSystemPlatform { IsMacOS = true },
            new FixedPackageManagerDetector(new PackageManagerInfo("Homebrew", true, "brew")),
            provider);

        var plan = await service.BuildPlanAsync(new SetupPlanRequest(ReactNativeVersion: "0.77.x"));

        Assert.Equal("0.77.x", plan.ToolchainProfile?.ReactNativeVersion);
        Assert.Equal("0.77.x", provider.LastLookup);
        Assert.Contains(plan.Items, item =>
            item.DependencyName == "Java"
            && item.Steps.Contains("Profile recommendation for React Native 0.77.x: Java supported range 17-21."));
    }

    [Fact]
    public async Task BuildPlanAsyncThrowsWhenProfileLookupFails()
    {
        var service = new SetupPlanService(
            new FakeDependencyCheckService(),
            new FakeSystemPlatform { IsMacOS = true },
            new FixedPackageManagerDetector(PackageManagerInfo.NotDetected()),
            new FixedToolchainProfileProvider("Unsupported React Native toolchain profile: 0.99.x."));

        var exception = await Assert.ThrowsAsync<SetupPlanException>(
            () => service.BuildPlanAsync(new SetupPlanRequest(ReactNativeVersion: "0.99.x")));

        Assert.Contains("Unsupported React Native toolchain profile: 0.99.x.", exception.Errors);
    }

    private sealed class FixedPackageManagerDetector : IPackageManagerDetector
    {
        private readonly PackageManagerInfo _packageManager;

        public FixedPackageManagerDetector(PackageManagerInfo packageManager)
        {
            _packageManager = packageManager;
        }

        public Task<PackageManagerInfo> DetectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_packageManager);
        }
    }

    private sealed class FixedToolchainProfileProvider : IToolchainProfileProvider
    {
        private readonly ToolchainProfile? _profile;
        private readonly string[] _errors;

        public FixedToolchainProfileProvider(ToolchainProfile profile)
        {
            _profile = profile;
            _errors = [];
        }

        public FixedToolchainProfileProvider(params string[] errors)
        {
            _profile = null;
            _errors = errors;
        }

        public string? LastLookup { get; private set; }

        public IReadOnlyList<ToolchainProfile> GetProfiles()
        {
            return _profile is null ? [] : [_profile];
        }

        public ToolchainProfileLookupResult GetDefaultProfile()
        {
            return _profile is null
                ? ToolchainProfileLookupResult.Failed(_errors)
                : ToolchainProfileLookupResult.Found(_profile);
        }

        public ToolchainProfileLookupResult GetProfile(string profileIdOrReactNativeVersion)
        {
            LastLookup = profileIdOrReactNativeVersion;

            return _profile is null
                ? ToolchainProfileLookupResult.Failed(_errors)
                : ToolchainProfileLookupResult.Found(_profile);
        }
    }

    private static ToolchainProfile CreateProfile(
        string id = "react-native-stable",
        string reactNativeVersion = "0.76.x",
        string javaRange = "17-21")
    {
        return new ToolchainProfile(
            id,
            "React Native Stable",
            reactNativeVersion,
            IsDefault: true,
            NodeJs: new ToolchainRequirement("Node.js", ToolchainRecommendationStrategy.Lts),
            Java: new ToolchainRequirement("Java", ToolchainRecommendationStrategy.SupportedRange, SupportedVersionRange: javaRange),
            Xcode: new ToolchainRequirement("Xcode", ToolchainRecommendationStrategy.MinimumVersion, MinimumVersion: "15.0"),
            CocoaPods: new ToolchainRequirement("CocoaPods", ToolchainRecommendationStrategy.MinimumVersion, MinimumVersion: "1.14.0"),
            Watchman: new ToolchainRequirement("Watchman", ToolchainRecommendationStrategy.LatestStable, Required: false),
            Android: new AndroidToolchainRequirement(
                CompileSdk: 35,
                TargetSdk: 35,
                MinSdk: 23,
                PlatformTools: new ToolchainRequirement("Android Platform Tools", ToolchainRecommendationStrategy.LatestStable),
                RequiredPackages: ["platforms;android-35", "platform-tools"]));
    }
}
