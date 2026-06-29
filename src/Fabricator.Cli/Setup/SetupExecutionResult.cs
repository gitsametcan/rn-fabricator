namespace Fabricator.Cli.Setup;

public enum SetupExecutionStepStatus
{
    Succeeded,
    Failed,
    SkippedByUser,
    SkippedByPolicy,
    ManualOnly,
    WouldRun
}

public sealed record SetupExecutionStepResult(
    string DependencyName,
    SetupExecutionStepStatus Status,
    string? CommandText = null,
    int? ExitCode = null,
    string? Reason = null,
    string? DiagnosticOutput = null);

public sealed class SetupExecutionResult
{
    public SetupExecutionResult(IReadOnlyList<SetupExecutionStepResult> steps)
    {
        Steps = steps;
    }

    public IReadOnlyList<SetupExecutionStepResult> Steps { get; }

    public int SucceededCount => Count(SetupExecutionStepStatus.Succeeded);

    public int FailedCount => Count(SetupExecutionStepStatus.Failed);

    public int SkippedByUserCount => Count(SetupExecutionStepStatus.SkippedByUser);

    public int SkippedByPolicyCount => Count(SetupExecutionStepStatus.SkippedByPolicy);

    public int ManualOnlyCount => Count(SetupExecutionStepStatus.ManualOnly);

    public int WouldRunCount => Count(SetupExecutionStepStatus.WouldRun);

    public bool HasFailures => FailedCount > 0;

    public bool HasManualOrPolicySkippedWork => ManualOnlyCount > 0 || SkippedByPolicyCount > 0;

    private int Count(SetupExecutionStepStatus status)
    {
        return Steps.Count(step => step.Status == status);
    }
}
