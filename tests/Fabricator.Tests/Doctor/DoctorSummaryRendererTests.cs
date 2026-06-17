using Fabricator.Cli.Doctor;
using Fabricator.Core.Environment;

namespace Fabricator.Tests.Doctor;

public sealed class DoctorSummaryRendererTests
{
    [Fact]
    public void RenderWritesStatusMarkersVersionsHintsAndSummary()
    {
        var summary = new DependencyCheckSummary(
        [
            DependencyCheckResult.Passed("Node.js", "v20.11.1", "Node.js is installed: v20.11.1"),
            DependencyCheckResult.Warning("Watchman", null, "Watchman was not found.", "Install Watchman."),
            DependencyCheckResult.Failed("Git", null, "Git was not found.", "Install Git.")
        ]);
        using var writer = new StringWriter();
        var renderer = new DoctorSummaryRenderer(writer);

        renderer.Render(summary);

        var output = writer.ToString();
        Assert.Contains("React Native environment checks", output);
        Assert.Contains("[PASS] Node.js (v20.11.1): Node.js is installed: v20.11.1", output);
        Assert.Contains("[WARN] Watchman: Watchman was not found.", output);
        Assert.Contains("Hint: Install Watchman.", output);
        Assert.Contains("[FAIL] Git: Git was not found.", output);
        Assert.Contains("Hint: Install Git.", output);
        Assert.Contains("Passed: 1, Warnings: 1, Failed: 1", output);
    }

    [Fact]
    public void RenderWritesMultilineHintsAsReadableSteps()
    {
        var summary = new DependencyCheckSummary(
        [
            DependencyCheckResult.Failed(
                "Android SDK",
                null,
                "Android SDK environment variables were not found.",
                "Install Android Studio.\nSet ANDROID_HOME.")
        ]);
        using var writer = new StringWriter();
        var renderer = new DoctorSummaryRenderer(writer);

        renderer.Render(summary);

        var output = writer.ToString();
        Assert.Contains("Hint:", output);
        Assert.Contains("- Install Android Studio.", output);
        Assert.Contains("- Set ANDROID_HOME.", output);
    }
}
