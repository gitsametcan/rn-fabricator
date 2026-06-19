using Fabricator.Core.Processes;

namespace Fabricator.Core.Environment;

public sealed class DependencyCheckService : IDependencyCheckService
{
    private const string AndroidHomeVariableName = "ANDROID_HOME";
    private const string AndroidSdkRootVariableName = "ANDROID_SDK_ROOT";

    private readonly IProcessRunner _processRunner;
    private readonly ISystemPlatform _systemPlatform;
    private readonly IEnvironmentVariables _environmentVariables;

    public DependencyCheckService(IProcessRunner processRunner)
        : this(processRunner, new SystemPlatform(), new SystemEnvironmentVariables())
    {
    }

    public DependencyCheckService(
        IProcessRunner processRunner,
        ISystemPlatform systemPlatform)
        : this(processRunner, systemPlatform, new SystemEnvironmentVariables())
    {
    }

    public DependencyCheckService(
        IProcessRunner processRunner,
        ISystemPlatform systemPlatform,
        IEnvironmentVariables environmentVariables)
    {
        _processRunner = processRunner;
        _systemPlatform = systemPlatform;
        _environmentVariables = environmentVariables;
    }

    public async Task<DependencyCheckSummary> CheckCoreToolsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DependencyCheckResult>();

        foreach (var definition in CreateCoreToolDefinitions())
        {
            results.Add(await CheckRequiredToolAsync(definition, cancellationToken));
        }

        return new DependencyCheckSummary(results);
    }

    public async Task<DependencyCheckSummary> CheckAndroidToolsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DependencyCheckResult>
        {
            await CheckToolAsync(CreateJavaDefinition(), cancellationToken),
            CheckAndroidSdk()
        };

        return new DependencyCheckSummary(results);
    }

    private DependencyCheckResult CheckAndroidSdk()
    {
        var androidHome = _environmentVariables.Get(AndroidHomeVariableName);
        if (!string.IsNullOrWhiteSpace(androidHome))
        {
            return DependencyCheckResult.Passed(
                "Android SDK",
                androidHome,
                $"{AndroidHomeVariableName} is configured.");
        }

        var androidSdkRoot = _environmentVariables.Get(AndroidSdkRootVariableName);
        if (!string.IsNullOrWhiteSpace(androidSdkRoot))
        {
            return DependencyCheckResult.Passed(
                "Android SDK",
                androidSdkRoot,
                $"{AndroidSdkRootVariableName} is configured.");
        }

        return DependencyCheckResult.Failed(
            "Android SDK",
            null,
            "Android SDK environment variables were not found.",
            CreateAndroidSdkHint());
    }

    public async Task<DependencyCheckSummary> CheckAppleToolsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DependencyCheckResult>
        {
            await CheckToolAsync(CreateWatchmanDefinition(), cancellationToken)
        };

        var macOnlyToolDefinitions = CreateMacOnlyToolDefinitions();
        if (!_systemPlatform.IsMacOS)
        {
            results.AddRange(macOnlyToolDefinitions.Select(CreateNonMacOSWarning));
            return new DependencyCheckSummary(results);
        }

        foreach (var definition in macOnlyToolDefinitions)
        {
            results.Add(await CheckToolAsync(definition, cancellationToken));
        }

        return new DependencyCheckSummary(results);
    }

    private DependencyCheckDefinition[] CreateCoreToolDefinitions()
    {
        return
        [
            new(
                "Node.js",
                "node",
                ["--version"],
                CreateNodeHint()),
            new(
                "npm",
                "npm",
                ["--version"],
                CreateNpmHint()),
            new(
                "Git",
                "git",
                ["--version"],
                CreateGitHint())
        ];
    }

    private DependencyCheckDefinition CreateWatchmanDefinition()
    {
        return new DependencyCheckDefinition(
            "Watchman",
            "watchman",
            ["--version"],
            CreateWatchmanHint(),
            DependencyCheckStatus.Warning);
    }

    private DependencyCheckDefinition[] CreateMacOnlyToolDefinitions()
    {
        return
        [
            new(
                "Xcode",
                "xcodebuild",
                ["-version"],
                CreateXcodeHint()),
            new(
                "CocoaPods",
                "pod",
                ["--version"],
                CreateCocoaPodsHint())
        ];
    }

    private DependencyCheckDefinition CreateJavaDefinition()
    {
        return new DependencyCheckDefinition(
            "Java",
            "java",
            ["-version"],
            CreateJavaHint());
    }

    private static DependencyCheckResult CreateNonMacOSWarning(DependencyCheckDefinition definition)
    {
        return DependencyCheckResult.Warning(
            definition.Name,
            null,
            $"{definition.Name} check is only available on macOS.",
            "Run this check on macOS when preparing React Native iOS builds.");
    }

    private string CreateNodeHint()
    {
        if (_systemPlatform.IsMacOS)
        {
            return CreateHint(
                "Install Node.js LTS from https://nodejs.org/ or run `brew install node`.",
                "Restart your terminal and verify with `node --version`.");
        }

        if (_systemPlatform.IsWindows)
        {
            return CreateHint(
                "Install Node.js LTS from https://nodejs.org/ or run `winget install OpenJS.NodeJS.LTS`.",
                "Restart PowerShell and verify with `node --version`.");
        }

        if (_systemPlatform.IsLinux)
        {
            return CreateHint(
                "Install Node.js LTS from https://nodejs.org/ or your distribution package manager.",
                "On Ubuntu, `sudo apt-get install nodejs npm` is a common starting point.",
                "Restart your shell and verify with `node --version`.");
        }

        return CreateHint(
            "Install Node.js LTS from https://nodejs.org/.",
            "Restart your terminal and verify with `node --version`.");
    }

    private string CreateNpmHint()
    {
        if (_systemPlatform.IsMacOS)
        {
            return CreateHint(
                "npm is bundled with Node.js; install Node.js LTS from https://nodejs.org/ or run `brew install node`.",
                "Verify with `npm --version`.");
        }

        if (_systemPlatform.IsWindows)
        {
            return CreateHint(
                "npm is bundled with Node.js; install Node.js LTS from https://nodejs.org/ or run `winget install OpenJS.NodeJS.LTS`.",
                "Restart PowerShell and verify with `npm --version`.");
        }

        if (_systemPlatform.IsLinux)
        {
            return CreateHint(
                "Install npm with Node.js from https://nodejs.org/ or your distribution package manager.",
                "On Ubuntu, `sudo apt-get install nodejs npm` is a common starting point.",
                "Verify with `npm --version`.");
        }

        return CreateHint(
            "Install Node.js LTS from https://nodejs.org/; npm is bundled with Node.js.",
            "Verify with `npm --version`.");
    }

    private string CreateGitHint()
    {
        if (_systemPlatform.IsMacOS)
        {
            return CreateHint(
                "Install Git with `xcode-select --install` or `brew install git`.",
                "Verify with `git --version`.");
        }

        if (_systemPlatform.IsWindows)
        {
            return CreateHint(
                "Install Git from https://git-scm.com/download/win or run `winget install Git.Git`.",
                "Restart PowerShell and verify with `git --version`.");
        }

        if (_systemPlatform.IsLinux)
        {
            return CreateHint(
                "Install Git with your distribution package manager.",
                "On Ubuntu, run `sudo apt-get install git`.",
                "Verify with `git --version`.");
        }

        return CreateHint(
            "Install Git from https://git-scm.com/downloads.",
            "Verify with `git --version`.");
    }

    private string CreateWatchmanHint()
    {
        if (_systemPlatform.IsMacOS)
        {
            return CreateHint(
                "Install Watchman with `brew install watchman`.",
                "Verify with `watchman --version`.");
        }

        if (_systemPlatform.IsWindows)
        {
            return CreateHint(
                "Watchman is optional for React Native on Windows; continue if Android builds work.",
                "If your workflow requires it, follow the Watchman install guide: https://facebook.github.io/watchman/docs/install.");
        }

        if (_systemPlatform.IsLinux)
        {
            return CreateHint(
                "Install Watchman from your package manager when available.",
                "If it is not packaged for your distribution, follow https://facebook.github.io/watchman/docs/install.",
                "Verify with `watchman --version`.");
        }

        return CreateHint(
            "Install Watchman if your React Native workflow needs it: https://facebook.github.io/watchman/docs/install.",
            "Verify with `watchman --version`.");
    }

    private static string CreateXcodeHint()
    {
        return CreateHint(
            "Install Xcode from the App Store.",
            "Run `sudo xcode-select --switch /Applications/Xcode.app`.",
            "Run `sudo xcodebuild -runFirstLaunch` and verify with `xcodebuild -version`.");
    }

    private static string CreateCocoaPodsHint()
    {
        return CreateHint(
            "Install CocoaPods with `brew install cocoapods` on macOS when Homebrew is available.",
            "Alternatively install with `sudo gem install cocoapods` or your preferred Ruby environment.",
            "Verify with `pod --version`.");
    }

    private string CreateJavaHint()
    {
        if (_systemPlatform.IsMacOS)
        {
            return CreateHint(
                "Install a JDK with `brew install --cask temurin` or from https://adoptium.net/.",
                "Restart your terminal and verify with `java -version`.");
        }

        if (_systemPlatform.IsWindows)
        {
            return CreateHint(
                "Install a JDK with `winget install EclipseAdoptium.Temurin.17.JDK` or from https://adoptium.net/.",
                "Restart PowerShell and verify with `java -version`.");
        }

        if (_systemPlatform.IsLinux)
        {
            return CreateHint(
                "Install a JDK with your distribution package manager.",
                "On Ubuntu, run `sudo apt-get install openjdk-17-jdk`.",
                "Verify with `java -version`.");
        }

        return CreateHint(
            "Install a supported JDK from https://adoptium.net/.",
            "Verify with `java -version`.");
    }

    private string CreateAndroidSdkHint()
    {
        if (_systemPlatform.IsMacOS)
        {
            return CreateHint(
                "Install Android Studio from https://developer.android.com/studio.",
                "Open SDK Manager and install Android SDK Platform and Android SDK Platform-Tools.",
                "Add `export ANDROID_HOME=\"$HOME/Library/Android/sdk\"` and `export PATH=\"$PATH:$ANDROID_HOME/platform-tools\"` to your shell profile.",
                "Restart your terminal and verify with `echo $ANDROID_HOME`.");
        }

        if (_systemPlatform.IsWindows)
        {
            return CreateHint(
                "Install Android Studio from https://developer.android.com/studio or run `winget install Google.AndroidStudio`.",
                "Open SDK Manager and install Android SDK Platform and Android SDK Platform-Tools.",
                "Set `ANDROID_HOME` to `%LOCALAPPDATA%\\Android\\Sdk` and add `%ANDROID_HOME%\\platform-tools` to PATH.",
                "Restart PowerShell and verify with `echo $env:ANDROID_HOME`.");
        }

        if (_systemPlatform.IsLinux)
        {
            return CreateHint(
                "Install Android Studio from https://developer.android.com/studio.",
                "Open SDK Manager and install Android SDK Platform and Android SDK Platform-Tools.",
                "Set `ANDROID_HOME` to your SDK path, commonly `$HOME/Android/Sdk`, and add `$ANDROID_HOME/platform-tools` to PATH.",
                "Restart your shell and verify with `echo $ANDROID_HOME`.");
        }

        return CreateHint(
            "Install Android Studio from https://developer.android.com/studio.",
            $"Set {AndroidHomeVariableName} to your Android SDK path and add platform-tools to PATH.");
    }

    private static string CreateHint(params string[] steps)
    {
        return string.Join(System.Environment.NewLine, steps);
    }

    private async Task<DependencyCheckResult> CheckRequiredToolAsync(
        DependencyCheckDefinition definition,
        CancellationToken cancellationToken)
    {
        return await CheckToolAsync(definition, cancellationToken);
    }

    private async Task<DependencyCheckResult> CheckToolAsync(
        DependencyCheckDefinition definition,
        CancellationToken cancellationToken)
    {
        var processResult = await _processRunner.RunAsync(
            new ProcessRunRequest(
                definition.FileName,
                definition.Arguments),
            cancellationToken);

        if (!processResult.Succeeded)
        {
            var message = $"{definition.Name} was not found or returned exit code {processResult.ExitCode}.";

            return definition.MissingStatus == DependencyCheckStatus.Warning
                ? DependencyCheckResult.Warning(definition.Name, null, message, definition.RemediationHint)
                : DependencyCheckResult.Failed(definition.Name, null, message, definition.RemediationHint);
        }

        var detectedVersion = ExtractDetectedVersion(
            string.IsNullOrWhiteSpace(processResult.StandardOutput)
                ? processResult.StandardError
                : processResult.StandardOutput);

        return DependencyCheckResult.Passed(
            definition.Name,
            detectedVersion,
            detectedVersion is null
                ? $"{definition.Name} is installed."
                : $"{definition.Name} is installed: {detectedVersion}");
    }

    private static string? ExtractDetectedVersion(string standardOutput)
    {
        var detectedVersion = standardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (detectedVersion is null)
        {
            return null;
        }

        const string gitVersionPrefix = "git version ";
        if (detectedVersion.StartsWith(gitVersionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return detectedVersion[gitVersionPrefix.Length..].Trim();
        }

        return detectedVersion;
    }
}
