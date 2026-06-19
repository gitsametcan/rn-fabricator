namespace Fabricator.Cli.Setup;

public sealed class SetupExecutionResultRenderer
{
    private readonly TextWriter _writer;

    public SetupExecutionResultRenderer(TextWriter writer)
    {
        _writer = writer;
    }

    public void RenderCommandStart(string dependencyName, string commandText)
    {
        _writer.WriteLine($"[command] {dependencyName}");
        _writer.WriteLine($"Command: {commandText}");
    }

    public void RenderStep(SetupExecutionStepResult step)
    {
        switch (step.Status)
        {
            case SetupExecutionStepStatus.Succeeded:
                _writer.WriteLine($"[succeeded] {step.DependencyName}");
                WriteDiagnosticOutput(step.DiagnosticOutput);
                break;
            case SetupExecutionStepStatus.Failed:
                _writer.WriteLine($"[failed] {step.DependencyName}: exit code {step.ExitCode}");
                WriteDiagnosticOutput(step.DiagnosticOutput);
                break;
            case SetupExecutionStepStatus.SkippedByUser:
                _writer.WriteLine($"[skipped by user] {step.DependencyName}");
                break;
            case SetupExecutionStepStatus.SkippedByPolicy:
                _writer.WriteLine($"[skipped by policy] {step.DependencyName}: {step.Reason}");
                break;
            case SetupExecutionStepStatus.ManualOnly:
                _writer.WriteLine($"[manual] {step.DependencyName}: review plan output.");
                break;
            case SetupExecutionStepStatus.WouldRun:
                _writer.WriteLine($"[dry-run] {step.DependencyName}: would run {step.CommandText}");
                break;
            default:
                throw new InvalidOperationException($"Unknown setup execution step status: {step.Status}");
        }
    }

    public void RenderSummary(SetupExecutionResult result)
    {
        _writer.WriteLine();
        var summaryText =
            $"Apply summary: {result.SucceededCount} succeeded, {result.FailedCount} failed, {result.SkippedByUserCount} skipped by user, {result.SkippedByPolicyCount} skipped by policy, {result.ManualOnlyCount} manual";
        if (result.WouldRunCount > 0)
        {
            summaryText += $", {result.WouldRunCount} would run";
        }

        _writer.WriteLine($"{summaryText}.");
        if (result.HasManualOrPolicySkippedWork)
        {
            _writer.WriteLine("Manual or skipped steps may still be required before React Native development works.");
        }
    }

    private void WriteDiagnosticOutput(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _writer.WriteLine(value.Trim());
        }
    }
}
