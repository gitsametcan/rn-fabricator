using Fabricator.Core.Processes;

namespace Fabricator.Core.Environment;

public sealed class DependencyCheckService : IDependencyCheckService
{
    private static readonly DependencyCheckDefinition[] CoreToolDefinitions =
    [
        new(
            "Node.js",
            "node",
            ["--version"],
            "Install Node.js and make sure `node` is available on PATH."),
        new(
            "npm",
            "npm",
            ["--version"],
            "Install npm and make sure `npm` is available on PATH."),
        new(
            "Git",
            "git",
            ["--version"],
            "Install Git and make sure `git` is available on PATH.")
    ];

    private static readonly DependencyCheckDefinition WatchmanDefinition = new(
        "Watchman",
        "watchman",
        ["--version"],
        "Install Watchman for a better React Native development experience.",
        DependencyCheckStatus.Warning);

    private static readonly DependencyCheckDefinition[] MacOnlyToolDefinitions =
    [
        new(
            "Xcode",
            "xcodebuild",
            ["-version"],
            "Install Xcode from the App Store and run `sudo xcode-select --switch /Applications/Xcode.app`."),
        new(
            "CocoaPods",
            "pod",
            ["--version"],
            "Install CocoaPods with `sudo gem install cocoapods` or your preferred Ruby environment.")
    ];

    private static readonly DependencyCheckDefinition JavaDefinition = new(
        "Java",
        "java",
        ["-version"],
        "Install a supported JDK and make sure `java` is available on PATH.");

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

        foreach (var definition in CoreToolDefinitions)
        {
            results.Add(await CheckRequiredToolAsync(definition, cancellationToken));
        }

        return new DependencyCheckSummary(results);
    }

    public async Task<DependencyCheckSummary> CheckAndroidToolsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DependencyCheckResult>
        {
            await CheckToolAsync(JavaDefinition, cancellationToken),
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
            $"Set {AndroidHomeVariableName} or {AndroidSdkRootVariableName} to your Android SDK path.");
    }

    public async Task<DependencyCheckSummary> CheckAppleToolsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DependencyCheckResult>
        {
            await CheckToolAsync(WatchmanDefinition, cancellationToken)
        };

        if (!_systemPlatform.IsMacOS)
        {
            results.AddRange(MacOnlyToolDefinitions.Select(CreateNonMacOSWarning));
            return new DependencyCheckSummary(results);
        }

        foreach (var definition in MacOnlyToolDefinitions)
        {
            results.Add(await CheckToolAsync(definition, cancellationToken));
        }

        return new DependencyCheckSummary(results);
    }

    private static DependencyCheckResult CreateNonMacOSWarning(DependencyCheckDefinition definition)
    {
        return DependencyCheckResult.Warning(
            definition.Name,
            null,
            $"{definition.Name} check is only available on macOS.",
            "Run this check on macOS when preparing React Native iOS builds.");
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
