using Fabricator.Core.Environment;
using Fabricator.Core.Toolchains;

namespace Fabricator.Core.Setup;

public sealed class SetupPlanService : ISetupPlanService
{
    private readonly IDependencyCheckService _dependencyCheckService;
    private readonly ISystemPlatform _systemPlatform;
    private readonly IPackageManagerDetector _packageManagerDetector;
    private readonly IToolchainProfileProvider _toolchainProfileProvider;

    public SetupPlanService(
        IDependencyCheckService dependencyCheckService,
        ISystemPlatform systemPlatform,
        IPackageManagerDetector packageManagerDetector,
        IToolchainProfileProvider toolchainProfileProvider)
    {
        _dependencyCheckService = dependencyCheckService;
        _systemPlatform = systemPlatform;
        _packageManagerDetector = packageManagerDetector;
        _toolchainProfileProvider = toolchainProfileProvider;
    }

    public async Task<SetupPlan> BuildPlanAsync(
        SetupPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = ResolveToolchainProfile(request);
        var summaries = new[]
        {
            await _dependencyCheckService.CheckCoreToolsAsync(cancellationToken),
            await _dependencyCheckService.CheckAppleToolsAsync(cancellationToken),
            await _dependencyCheckService.CheckAndroidToolsAsync(cancellationToken)
        };
        var results = summaries
            .SelectMany(summary => summary.Results)
            .Where(result => result.Status != DependencyCheckStatus.Passed)
            .ToArray();
        var packageManager = await _packageManagerDetector.DetectAsync(cancellationToken);
        var missingDependencyNames = results.Select(result => result.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var items = new List<SetupPlanItem>();

        foreach (var result in results)
        {
            var item = CreatePlanItem(result, packageManager, missingDependencyNames, profile);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return new SetupPlan(
            GetPlatformName(),
            packageManager,
            items,
            new SetupPlanToolchainProfile(
                profile.Id,
                profile.DisplayName,
                profile.ReactNativeVersion,
                profile.IsDefault));
    }

    private SetupPlanItem? CreatePlanItem(
        DependencyCheckResult result,
        PackageManagerInfo packageManager,
        IReadOnlySet<string> missingDependencyNames,
        ToolchainProfile profile)
    {
        return result.Name switch
        {
            "Node.js" => CreateNodePlanItem(packageManager, profile),
            "npm" when missingDependencyNames.Contains("Node.js") => null,
            "npm" => CreateNpmPlanItem(packageManager),
            "Git" => CreateGitPlanItem(packageManager),
            "Watchman" => CreateWatchmanPlanItem(packageManager, profile),
            "Xcode" when _systemPlatform.IsMacOS => CreateXcodePlanItem(profile),
            "Xcode" => null,
            "CocoaPods" when _systemPlatform.IsMacOS => CreateCocoaPodsPlanItem(packageManager, profile),
            "CocoaPods" => null,
            "Java" => CreateJavaPlanItem(packageManager, profile),
            "Android SDK" => CreateAndroidSdkPlanItem(profile),
            _ => CreateManualPlanItem(result)
        };
    }

    private ToolchainProfile ResolveToolchainProfile(SetupPlanRequest request)
    {
        var result = request switch
        {
            { ProfileId: not null } => _toolchainProfileProvider.GetProfile(request.ProfileId),
            { ReactNativeVersion: not null } => _toolchainProfileProvider.GetProfile(request.ReactNativeVersion),
            _ => _toolchainProfileProvider.GetDefaultProfile()
        };

        if (!result.Succeeded)
        {
            throw new SetupPlanException(result.Errors);
        }

        return result.Profile!;
    }

    private SetupPlanItem CreateNodePlanItem(PackageManagerInfo packageManager, ToolchainProfile profile)
    {
        var recommendation = FormatRequirementRecommendation(profile.NodeJs, profile);

        if (_systemPlatform.IsMacOS && packageManager.IsAvailable)
        {
            return Command("Node.js", "Install Node.js and npm", [
                "brew install node",
                recommendation
            ]);
        }

        if (_systemPlatform.IsWindows && packageManager.IsAvailable)
        {
            return Command("Node.js", "Install Node.js and npm", [
                "winget install OpenJS.NodeJS.LTS",
                recommendation
            ]);
        }

        if (_systemPlatform.IsLinux && packageManager.IsAvailable)
        {
            return Command("Node.js", "Install Node.js and npm", [
                "sudo apt-get install nodejs npm",
                recommendation
            ], requiresAdmin: true);
        }

        return Manual("Node.js", "Install Node.js and npm manually", [
            "Install Node.js LTS from https://nodejs.org/.",
            recommendation,
            "Restart your terminal and verify with `node --version` and `npm --version`."
        ]);
    }

    private SetupPlanItem CreateNpmPlanItem(PackageManagerInfo packageManager)
    {
        if (_systemPlatform.IsLinux && packageManager.IsAvailable)
        {
            return Command("npm", "Install npm", ["sudo apt-get install npm"], requiresAdmin: true);
        }

        return Manual("npm", "Repair npm installation", [
            "npm is bundled with Node.js.",
            "Reinstall Node.js LTS and verify with `npm --version`."
        ]);
    }

    private SetupPlanItem CreateGitPlanItem(PackageManagerInfo packageManager)
    {
        if (_systemPlatform.IsMacOS && packageManager.IsAvailable)
        {
            return Command("Git", "Install Git", ["brew install git"]);
        }

        if (_systemPlatform.IsWindows && packageManager.IsAvailable)
        {
            return Command("Git", "Install Git", ["winget install Git.Git"]);
        }

        if (_systemPlatform.IsLinux && packageManager.IsAvailable)
        {
            return Command("Git", "Install Git", ["sudo apt-get install git"], requiresAdmin: true);
        }

        return Manual("Git", "Install Git manually", [
            "Install Git from https://git-scm.com/downloads.",
            "Restart your terminal and verify with `git --version`."
        ]);
    }

    private SetupPlanItem CreateWatchmanPlanItem(PackageManagerInfo packageManager, ToolchainProfile profile)
    {
        var recommendation = FormatRequirementRecommendation(profile.Watchman, profile);

        if (_systemPlatform.IsMacOS && packageManager.IsAvailable)
        {
            return Command("Watchman", "Install Watchman", [
                "brew install watchman",
                recommendation
            ]);
        }

        return Manual("Watchman", "Review Watchman setup", [
            "Watchman is recommended but optional for many React Native workflows.",
            recommendation,
            "Install it from https://facebook.github.io/watchman/docs/install if your platform supports it."
        ]);
    }

    private static SetupPlanItem CreateXcodePlanItem(ToolchainProfile profile)
    {
        return Manual("Xcode", "Install and select Xcode", [
            "Install Xcode from the App Store.",
            FormatRequirementRecommendation(profile.Xcode, profile),
            "Run `sudo xcode-select --switch /Applications/Xcode.app`.",
            "Run `sudo xcodebuild -runFirstLaunch`."
        ]);
    }

    private static SetupPlanItem CreateCocoaPodsPlanItem(
        PackageManagerInfo packageManager,
        ToolchainProfile profile)
    {
        var recommendation = FormatRequirementRecommendation(profile.CocoaPods, profile);

        if (packageManager.IsAvailable)
        {
            return Command("CocoaPods", "Install CocoaPods", [
                "brew install cocoapods",
                recommendation
            ]);
        }

        return Command(
            "CocoaPods",
            "Install CocoaPods",
            [
                "sudo gem install cocoapods",
                recommendation
            ],
            requiresAdmin: true);
    }

    private SetupPlanItem CreateJavaPlanItem(PackageManagerInfo packageManager, ToolchainProfile profile)
    {
        var recommendation = FormatRequirementRecommendation(profile.Java, profile);

        if (_systemPlatform.IsMacOS && packageManager.IsAvailable)
        {
            return Command("Java", "Install Temurin JDK", [
                "brew install --cask temurin",
                recommendation
            ]);
        }

        if (_systemPlatform.IsWindows && packageManager.IsAvailable)
        {
            return Command("Java", "Install Temurin JDK", [
                "winget install EclipseAdoptium.Temurin.17.JDK",
                recommendation
            ]);
        }

        if (_systemPlatform.IsLinux && packageManager.IsAvailable)
        {
            return Command("Java", "Install OpenJDK", [
                "sudo apt-get install openjdk-17-jdk",
                recommendation
            ], requiresAdmin: true);
        }

        return Manual("Java", "Install a supported JDK manually", [
            "Install a supported JDK from https://adoptium.net/.",
            recommendation,
            "Restart your terminal and verify with `java -version`."
        ]);
    }

    private SetupPlanItem CreateAndroidSdkPlanItem(ToolchainProfile profile)
    {
        var androidRecommendation = FormatAndroidRecommendation(profile);

        if (_systemPlatform.IsMacOS)
        {
            return Environment("Android SDK", "Configure Android SDK", [
                "Install Android Studio from https://developer.android.com/studio.",
                androidRecommendation,
                "Add `export ANDROID_HOME=\"$HOME/Library/Android/sdk\"` to your shell profile.",
                "Add `export PATH=\"$PATH:$ANDROID_HOME/platform-tools\"` to your shell profile."
            ]);
        }

        if (_systemPlatform.IsWindows)
        {
            return Environment("Android SDK", "Configure Android SDK", [
                "Install Android Studio from https://developer.android.com/studio.",
                androidRecommendation,
                "Set ANDROID_HOME to `%LOCALAPPDATA%\\Android\\Sdk`.",
                "Add `%ANDROID_HOME%\\platform-tools` to PATH."
            ]);
        }

        if (_systemPlatform.IsLinux)
        {
            return Environment("Android SDK", "Configure Android SDK", [
                "Install Android Studio from https://developer.android.com/studio.",
                androidRecommendation,
                "Set ANDROID_HOME to your SDK path, commonly `$HOME/Android/Sdk`.",
                "Add `$ANDROID_HOME/platform-tools` to PATH."
            ]);
        }

        return Environment("Android SDK", "Configure Android SDK", [
            "Install Android Studio from https://developer.android.com/studio.",
            androidRecommendation,
            "Set ANDROID_HOME to your Android SDK path.",
            "Add platform-tools to PATH."
        ]);
    }

    private static SetupPlanItem CreateManualPlanItem(DependencyCheckResult result)
    {
        return Manual(result.Name, $"Resolve {result.Name}", [
            result.RemediationHint ?? result.Message
        ]);
    }

    private string GetPlatformName()
    {
        if (_systemPlatform.IsMacOS)
        {
            return "macOS";
        }

        if (_systemPlatform.IsWindows)
        {
            return "Windows";
        }

        return _systemPlatform.IsLinux ? "Linux" : "Unknown";
    }

    private static SetupPlanItem Manual(string dependencyName, string title, IReadOnlyList<string> steps)
    {
        return new SetupPlanItem(dependencyName, SetupPlanItemKind.Manual, title, steps);
    }

    private static SetupPlanItem Command(
        string dependencyName,
        string title,
        IReadOnlyList<string> steps,
        bool requiresAdmin = false)
    {
        return new SetupPlanItem(dependencyName, SetupPlanItemKind.Command, title, steps, requiresAdmin);
    }

    private static SetupPlanItem Environment(string dependencyName, string title, IReadOnlyList<string> steps)
    {
        return new SetupPlanItem(dependencyName, SetupPlanItemKind.Environment, title, steps);
    }

    private static string FormatRequirementRecommendation(
        ToolchainRequirement requirement,
        ToolchainProfile profile)
    {
        var recommendation = requirement.Strategy switch
        {
            ToolchainRecommendationStrategy.ExactVersion => $"recommended version {requirement.RecommendedVersion}",
            ToolchainRecommendationStrategy.MinimumVersion => $"minimum version {requirement.MinimumVersion}",
            ToolchainRecommendationStrategy.SupportedRange => $"supported range {requirement.SupportedVersionRange}",
            ToolchainRecommendationStrategy.LatestStable => "latest stable version",
            ToolchainRecommendationStrategy.Lts => "active LTS version",
            ToolchainRecommendationStrategy.Manual => "manual setup",
            _ => "profile recommendation"
        };

        return $"Profile recommendation for React Native {profile.ReactNativeVersion}: {requirement.Name} {recommendation}.";
    }

    private static string FormatAndroidRecommendation(ToolchainProfile profile)
    {
        var targetSdk = profile.Android.TargetSdk?.ToString() ?? "not specified";
        var minSdk = profile.Android.MinSdk?.ToString() ?? "not specified";
        var packages = profile.Android.RequiredPackages.Count > 0
            ? $" Required SDK packages: {string.Join(", ", profile.Android.RequiredPackages)}."
            : string.Empty;

        return $"Profile recommendation for React Native {profile.ReactNativeVersion}: Android compile SDK {profile.Android.CompileSdk}, target SDK {targetSdk}, min SDK {minSdk}.{packages}";
    }
}
