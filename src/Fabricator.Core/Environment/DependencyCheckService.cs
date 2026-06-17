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

    private readonly IProcessRunner _processRunner;
    private readonly ISystemPlatform _systemPlatform;

    public DependencyCheckService(IProcessRunner processRunner)
        : this(processRunner, new SystemPlatform())
    {
    }

    public DependencyCheckService(
        IProcessRunner processRunner,
        ISystemPlatform systemPlatform)
    {
        _processRunner = processRunner;
        _systemPlatform = systemPlatform;
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

        var detectedVersion = ExtractDetectedVersion(processResult.StandardOutput);

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
