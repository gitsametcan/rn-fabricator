namespace Fabricator.Core.Environment;

public sealed class DependencyCheckSummary
{
    public DependencyCheckSummary(IReadOnlyList<DependencyCheckResult> results)
    {
        Results = results;
    }

    public IReadOnlyList<DependencyCheckResult> Results { get; }

    public int PassedCount => Results.Count(result => result.Status == DependencyCheckStatus.Passed);

    public int WarningCount => Results.Count(result => result.Status == DependencyCheckStatus.Warning);

    public int FailedCount => Results.Count(result => result.Status == DependencyCheckStatus.Failed);

    public bool HasRequiredFailures => FailedCount > 0;

    public int ExitCode => HasRequiredFailures ? ExitCodes.EnvironmentFailure : ExitCodes.Success;
}
