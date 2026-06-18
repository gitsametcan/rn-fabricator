using Fabricator.Core.Setup;

namespace Fabricator.Cli.Setup;

public sealed class SetupPlanRenderer
{
    private readonly TextWriter _writer;

    public SetupPlanRenderer(TextWriter writer)
    {
        _writer = writer;
    }

    public void Render(SetupPlan plan, bool includeReadOnlyFooter = true)
    {
        _writer.WriteLine("React Native setup plan");
        _writer.WriteLine();
        _writer.WriteLine($"Platform: {plan.PlatformName}");
        if (plan.ToolchainProfile is not null)
        {
            _writer.WriteLine($"Toolchain profile: {plan.ToolchainProfile.DisplayName} ({plan.ToolchainProfile.Id})");
            _writer.WriteLine($"React Native: {plan.ToolchainProfile.ReactNativeVersion}");
        }

        _writer.WriteLine($"Package manager: {FormatPackageManager(plan.PackageManager)}");
        _writer.WriteLine();

        if (!plan.HasItems)
        {
            _writer.WriteLine("No setup actions needed. Your environment checks passed.");
            return;
        }

        foreach (var item in plan.Items)
        {
            RenderItem(item);
            _writer.WriteLine();
        }

        _writer.WriteLine(
            $"Summary: {plan.CommandCount} command, {plan.ManualCount} manual, {plan.EnvironmentCount} environment step(s).");
        if (includeReadOnlyFooter)
        {
            _writer.WriteLine("No install commands were executed. Review the plan before running any setup commands.");
        }
    }

    private void RenderItem(SetupPlanItem item)
    {
        var marker = item.Kind switch
        {
            SetupPlanItemKind.Command => "[command]",
            SetupPlanItemKind.Environment => "[environment]",
            SetupPlanItemKind.Manual => "[manual]",
            _ => "[unknown]"
        };
        var adminSuffix = item.RequiresAdmin ? " (may require admin privileges)" : string.Empty;

        _writer.WriteLine($"{marker} {item.DependencyName}: {item.Title}{adminSuffix}");
        foreach (var step in item.Steps)
        {
            _writer.WriteLine($"  - {step}");
        }
    }

    private static string FormatPackageManager(PackageManagerInfo packageManager)
    {
        return packageManager.IsAvailable
            ? $"{packageManager.Name} ({packageManager.CommandName})"
            : "not detected; manual guidance will be used";
    }
}
