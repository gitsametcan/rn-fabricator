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

    private readonly IProcessRunner _processRunner;

    public DependencyCheckService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
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

    private async Task<DependencyCheckResult> CheckRequiredToolAsync(
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
            return DependencyCheckResult.Failed(
                definition.Name,
                null,
                $"{definition.Name} was not found or returned exit code {processResult.ExitCode}.",
                definition.RemediationHint);
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
