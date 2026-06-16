namespace Fabricator.Core.Environment;

public sealed record DependencyCheckResult(
    string Name,
    DependencyCheckStatus Status,
    string? DetectedVersion,
    string Message,
    string? RemediationHint)
{
    public bool IsRequiredFailure => Status == DependencyCheckStatus.Failed;

    public static DependencyCheckResult Passed(
        string name,
        string? detectedVersion,
        string message)
    {
        return new DependencyCheckResult(
            name,
            DependencyCheckStatus.Passed,
            detectedVersion,
            message,
            null);
    }

    public static DependencyCheckResult Warning(
        string name,
        string? detectedVersion,
        string message,
        string remediationHint)
    {
        return new DependencyCheckResult(
            name,
            DependencyCheckStatus.Warning,
            detectedVersion,
            message,
            remediationHint);
    }

    public static DependencyCheckResult Failed(
        string name,
        string? detectedVersion,
        string message,
        string remediationHint)
    {
        return new DependencyCheckResult(
            name,
            DependencyCheckStatus.Failed,
            detectedVersion,
            message,
            remediationHint);
    }
}
