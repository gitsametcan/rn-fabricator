using Fabricator.Cli.Setup;
using Fabricator.Core.Setup;

namespace Fabricator.Tests.Setup;

public sealed class SetupPlanRendererTests
{
    [Fact]
    public void RenderWritesPlatformPackageManagerItemsAndSummary()
    {
        var plan = new SetupPlan(
            "macOS",
            new PackageManagerInfo("Homebrew", true, "brew"),
            [
                new SetupPlanItem(
                    "Watchman",
                    SetupPlanItemKind.Command,
                    "Install Watchman",
                    ["brew install watchman"]),
                new SetupPlanItem(
                    "Xcode",
                    SetupPlanItemKind.Manual,
                    "Install and select Xcode",
                    ["Install Xcode from the App Store."]),
                new SetupPlanItem(
                    "Android SDK",
                    SetupPlanItemKind.Environment,
                    "Configure Android SDK",
                    ["export ANDROID_HOME=\"$HOME/Library/Android/sdk\""])
            ]);
        using var writer = new StringWriter();
        var renderer = new SetupPlanRenderer(writer);

        renderer.Render(plan);

        var output = writer.ToString();
        Assert.Contains("React Native setup plan", output);
        Assert.Contains("Platform: macOS", output);
        Assert.Contains("Package manager: Homebrew (brew)", output);
        Assert.Contains("[command] Watchman: Install Watchman", output);
        Assert.Contains("[manual] Xcode: Install and select Xcode", output);
        Assert.Contains("[environment] Android SDK: Configure Android SDK", output);
        Assert.Contains("Summary: 1 command, 1 manual, 1 environment step(s).", output);
        Assert.Contains("No install commands were executed.", output);
    }

    [Fact]
    public void RenderWritesNoActionsMessageForEmptyPlan()
    {
        var plan = new SetupPlan("Linux", PackageManagerInfo.NotDetected(), []);
        using var writer = new StringWriter();
        var renderer = new SetupPlanRenderer(writer);

        renderer.Render(plan);

        Assert.Contains("No setup actions needed.", writer.ToString());
    }
}
