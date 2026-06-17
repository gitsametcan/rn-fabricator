using Fabricator.Core.Environment;

namespace Fabricator.Core.Setup;

public sealed class SetupPlanService : ISetupPlanService
{
    private readonly IDependencyCheckService _dependencyCheckService;
    private readonly ISystemPlatform _systemPlatform;
    private readonly IPackageManagerDetector _packageManagerDetector;

    public SetupPlanService(
        IDependencyCheckService dependencyCheckService,
        ISystemPlatform systemPlatform,
        IPackageManagerDetector packageManagerDetector)
    {
        _dependencyCheckService = dependencyCheckService;
        _systemPlatform = systemPlatform;
        _packageManagerDetector = packageManagerDetector;
    }

    public async Task<SetupPlan> BuildPlanAsync(CancellationToken cancellationToken = default)
    {
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
            var item = CreatePlanItem(result, packageManager, missingDependencyNames);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return new SetupPlan(GetPlatformName(), packageManager, items);
    }

    private SetupPlanItem? CreatePlanItem(
        DependencyCheckResult result,
        PackageManagerInfo packageManager,
        IReadOnlySet<string> missingDependencyNames)
    {
        return result.Name switch
        {
            "Node.js" => CreateNodePlanItem(packageManager),
            "npm" when missingDependencyNames.Contains("Node.js") => null,
            "npm" => CreateNpmPlanItem(packageManager),
            "Git" => CreateGitPlanItem(packageManager),
            "Watchman" => CreateWatchmanPlanItem(packageManager),
            "Xcode" when _systemPlatform.IsMacOS => CreateXcodePlanItem(),
            "Xcode" => null,
            "CocoaPods" when _systemPlatform.IsMacOS => CreateCocoaPodsPlanItem(),
            "CocoaPods" => null,
            "Java" => CreateJavaPlanItem(packageManager),
            "Android SDK" => CreateAndroidSdkPlanItem(),
            _ => CreateManualPlanItem(result)
        };
    }

    private SetupPlanItem CreateNodePlanItem(PackageManagerInfo packageManager)
    {
        if (_systemPlatform.IsMacOS && packageManager.IsAvailable)
        {
            return Command("Node.js", "Install Node.js and npm", ["brew install node"]);
        }

        if (_systemPlatform.IsWindows && packageManager.IsAvailable)
        {
            return Command("Node.js", "Install Node.js and npm", ["winget install OpenJS.NodeJS.LTS"]);
        }

        if (_systemPlatform.IsLinux && packageManager.IsAvailable)
        {
            return Command("Node.js", "Install Node.js and npm", ["sudo apt-get install nodejs npm"], requiresAdmin: true);
        }

        return Manual("Node.js", "Install Node.js and npm manually", [
            "Install Node.js LTS from https://nodejs.org/.",
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

    private SetupPlanItem CreateWatchmanPlanItem(PackageManagerInfo packageManager)
    {
        if (_systemPlatform.IsMacOS && packageManager.IsAvailable)
        {
            return Command("Watchman", "Install Watchman", ["brew install watchman"]);
        }

        return Manual("Watchman", "Review Watchman setup", [
            "Watchman is recommended but optional for many React Native workflows.",
            "Install it from https://facebook.github.io/watchman/docs/install if your platform supports it."
        ]);
    }

    private static SetupPlanItem CreateXcodePlanItem()
    {
        return Manual("Xcode", "Install and select Xcode", [
            "Install Xcode from the App Store.",
            "Run `sudo xcode-select --switch /Applications/Xcode.app`.",
            "Run `sudo xcodebuild -runFirstLaunch`."
        ]);
    }

    private static SetupPlanItem CreateCocoaPodsPlanItem()
    {
        return Command(
            "CocoaPods",
            "Install CocoaPods",
            ["sudo gem install cocoapods"],
            requiresAdmin: true);
    }

    private SetupPlanItem CreateJavaPlanItem(PackageManagerInfo packageManager)
    {
        if (_systemPlatform.IsMacOS && packageManager.IsAvailable)
        {
            return Command("Java", "Install Temurin JDK", ["brew install --cask temurin"]);
        }

        if (_systemPlatform.IsWindows && packageManager.IsAvailable)
        {
            return Command("Java", "Install Temurin JDK", ["winget install EclipseAdoptium.Temurin.17.JDK"]);
        }

        if (_systemPlatform.IsLinux && packageManager.IsAvailable)
        {
            return Command("Java", "Install OpenJDK", ["sudo apt-get install openjdk-17-jdk"], requiresAdmin: true);
        }

        return Manual("Java", "Install a supported JDK manually", [
            "Install a supported JDK from https://adoptium.net/.",
            "Restart your terminal and verify with `java -version`."
        ]);
    }

    private SetupPlanItem CreateAndroidSdkPlanItem()
    {
        if (_systemPlatform.IsMacOS)
        {
            return Environment("Android SDK", "Configure Android SDK", [
                "Install Android Studio from https://developer.android.com/studio.",
                "Install Android SDK Platform and Android SDK Platform-Tools from SDK Manager.",
                "Add `export ANDROID_HOME=\"$HOME/Library/Android/sdk\"` to your shell profile.",
                "Add `export PATH=\"$PATH:$ANDROID_HOME/platform-tools\"` to your shell profile."
            ]);
        }

        if (_systemPlatform.IsWindows)
        {
            return Environment("Android SDK", "Configure Android SDK", [
                "Install Android Studio from https://developer.android.com/studio.",
                "Install Android SDK Platform and Android SDK Platform-Tools from SDK Manager.",
                "Set ANDROID_HOME to `%LOCALAPPDATA%\\Android\\Sdk`.",
                "Add `%ANDROID_HOME%\\platform-tools` to PATH."
            ]);
        }

        if (_systemPlatform.IsLinux)
        {
            return Environment("Android SDK", "Configure Android SDK", [
                "Install Android Studio from https://developer.android.com/studio.",
                "Install Android SDK Platform and Android SDK Platform-Tools from SDK Manager.",
                "Set ANDROID_HOME to your SDK path, commonly `$HOME/Android/Sdk`.",
                "Add `$ANDROID_HOME/platform-tools` to PATH."
            ]);
        }

        return Environment("Android SDK", "Configure Android SDK", [
            "Install Android Studio from https://developer.android.com/studio.",
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
}
