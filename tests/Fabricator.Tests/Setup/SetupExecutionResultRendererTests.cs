using Fabricator.Cli.Setup;

namespace Fabricator.Tests.Setup;

public sealed class SetupExecutionResultRendererTests
{
    [Fact]
    public void RenderStepWritesStableStatusLines()
    {
        using var writer = new StringWriter();
        var renderer = new SetupExecutionResultRenderer(writer);

        renderer.RenderStep(new SetupExecutionStepResult(
            "Watchman",
            SetupExecutionStepStatus.Succeeded,
            CommandText: "brew install watchman",
            ExitCode: 0,
            DiagnosticOutput: " installed "));
        renderer.RenderStep(new SetupExecutionStepResult(
            "Java",
            SetupExecutionStepStatus.Failed,
            CommandText: "brew install --cask temurin",
            ExitCode: 42,
            DiagnosticOutput: "install failed"));
        renderer.RenderStep(new SetupExecutionStepResult(
            "Git",
            SetupExecutionStepStatus.SkippedByUser));
        renderer.RenderStep(new SetupExecutionStepResult(
            "CocoaPods",
            SetupExecutionStepStatus.SkippedByPolicy,
            Reason: "command requires admin privileges."));
        renderer.RenderStep(new SetupExecutionStepResult(
            "Xcode",
            SetupExecutionStepStatus.ManualOnly));
        renderer.RenderStep(new SetupExecutionStepResult(
            "Node.js",
            SetupExecutionStepStatus.WouldRun,
            CommandText: "brew install node"));

        var output = writer.ToString();
        Assert.Contains("[succeeded] Watchman", output);
        Assert.Contains("installed", output);
        Assert.Contains("[failed] Java: exit code 42", output);
        Assert.Contains("install failed", output);
        Assert.Contains("[skipped by user] Git", output);
        Assert.Contains("[skipped by policy] CocoaPods: command requires admin privileges.", output);
        Assert.Contains("[manual] Xcode: review plan output.", output);
        Assert.Contains("[dry-run] Node.js: would run brew install node", output);
    }

    [Fact]
    public void RenderSummaryWritesCountsAndManualWorkWarning()
    {
        using var writer = new StringWriter();
        var renderer = new SetupExecutionResultRenderer(writer);
        var result = new SetupExecutionResult([
            new SetupExecutionStepResult("Watchman", SetupExecutionStepStatus.Succeeded),
            new SetupExecutionStepResult("Java", SetupExecutionStepStatus.Failed),
            new SetupExecutionStepResult("Git", SetupExecutionStepStatus.SkippedByUser),
            new SetupExecutionStepResult("CocoaPods", SetupExecutionStepStatus.SkippedByPolicy),
            new SetupExecutionStepResult("Xcode", SetupExecutionStepStatus.ManualOnly),
            new SetupExecutionStepResult("Node.js", SetupExecutionStepStatus.WouldRun)
        ]);

        renderer.RenderSummary(result);

        var output = writer.ToString();
        Assert.Contains(
            "Apply summary: 1 succeeded, 1 failed, 1 skipped by user, 1 skipped by policy, 1 manual, 1 would run.",
            output);
        Assert.Contains("Manual or skipped steps may still be required", output);
    }

    [Fact]
    public void RenderSummaryOmitsWouldRunWhenNoDryRunStepsExist()
    {
        using var writer = new StringWriter();
        var renderer = new SetupExecutionResultRenderer(writer);
        var result = new SetupExecutionResult([
            new SetupExecutionStepResult("Watchman", SetupExecutionStepStatus.Succeeded)
        ]);

        renderer.RenderSummary(result);

        var output = writer.ToString();
        Assert.Contains("Apply summary: 1 succeeded, 0 failed, 0 skipped by user, 0 skipped by policy, 0 manual.", output);
        Assert.DoesNotContain("would run", output);
        Assert.DoesNotContain("Manual or skipped steps may still be required", output);
    }
}
