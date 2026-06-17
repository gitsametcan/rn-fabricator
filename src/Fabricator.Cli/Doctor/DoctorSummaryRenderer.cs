using Fabricator.Core.Environment;

namespace Fabricator.Cli.Doctor;

public sealed class DoctorSummaryRenderer
{
    private readonly TextWriter _writer;

    public DoctorSummaryRenderer(TextWriter writer)
    {
        _writer = writer;
    }

    public void Render(DependencyCheckSummary summary)
    {
        _writer.WriteLine("React Native environment checks");
        _writer.WriteLine();

        foreach (var result in summary.Results)
        {
            RenderResult(result);
        }

        _writer.WriteLine();
        _writer.WriteLine($"Passed: {summary.PassedCount}, Warnings: {summary.WarningCount}, Failed: {summary.FailedCount}");
    }

    private void RenderResult(DependencyCheckResult result)
    {
        var marker = result.Status switch
        {
            DependencyCheckStatus.Passed => "[PASS]",
            DependencyCheckStatus.Warning => "[WARN]",
            DependencyCheckStatus.Failed => "[FAIL]",
            _ => "[UNKNOWN]"
        };

        var version = string.IsNullOrWhiteSpace(result.DetectedVersion)
            ? string.Empty
            : $" ({result.DetectedVersion})";

        _writer.WriteLine($"{marker} {result.Name}{version}: {result.Message}");

        if (!string.IsNullOrWhiteSpace(result.RemediationHint))
        {
            _writer.WriteLine($"       Hint: {result.RemediationHint}");
        }
    }
}
