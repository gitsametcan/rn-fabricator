using Fabricator.Core;
using Fabricator.Core.Processes;
using Fabricator.Core.Setup;

namespace Fabricator.Cli.Setup;

public sealed class SetupApplyCommandHandler
{
    private static readonly HashSet<string> SafeCommandAllowlist = new(StringComparer.Ordinal)
    {
        "brew install watchman",
        "brew install git",
        "brew install node",
        "brew install --cask temurin",
        "winget install OpenJS.NodeJS.LTS",
        "winget install Git.Git",
        "winget install EclipseAdoptium.Temurin.17.JDK"
    };

    private readonly ISetupPlanService _setupPlanService;
    private readonly SetupPlanRenderer _planRenderer;
    private readonly IProcessRunner _processRunner;
    private readonly TextReader _reader;
    private readonly TextWriter _writer;
    private readonly TextWriter _errorWriter;

    public SetupApplyCommandHandler(
        ISetupPlanService setupPlanService,
        SetupPlanRenderer planRenderer,
        IProcessRunner processRunner,
        TextReader reader,
        TextWriter writer,
        TextWriter? errorWriter = null)
    {
        _setupPlanService = setupPlanService;
        _planRenderer = planRenderer;
        _processRunner = processRunner;
        _reader = reader;
        _writer = writer;
        _errorWriter = errorWriter ?? Console.Error;
    }

    public async Task<int> RunAsync(
        string? profileId = null,
        string? reactNativeVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(profileId) && !string.IsNullOrWhiteSpace(reactNativeVersion))
        {
            _errorWriter.WriteLine("Choose either --profile or --react-native, not both.");
            return ExitCodes.InvalidInput;
        }

        SetupPlan plan;

        try
        {
            plan = await _setupPlanService.BuildPlanAsync(
                new SetupPlanRequest(
                    Normalize(profileId),
                    Normalize(reactNativeVersion)),
                cancellationToken);
        }
        catch (SetupPlanException exception)
        {
            _errorWriter.WriteLine("Invalid setup apply input:");
            foreach (var error in exception.Errors)
            {
                _errorWriter.WriteLine($"- {error}");
            }

            return ExitCodes.InvalidInput;
        }

        _planRenderer.Render(plan, includeReadOnlyFooter: false);
        _writer.WriteLine();
        _writer.WriteLine("Setup apply");

        var summary = new SetupApplySummary();

        foreach (var item in plan.Items)
        {
            await ApplyItemAsync(item, summary, cancellationToken);
        }

        RenderSummary(summary);

        return summary.Failed > 0 ? ExitCodes.GeneralFailure : ExitCodes.Success;
    }

    private async Task ApplyItemAsync(
        SetupPlanItem item,
        SetupApplySummary summary,
        CancellationToken cancellationToken)
    {
        if (item.Kind != SetupPlanItemKind.Command)
        {
            summary.Manual++;
            _writer.WriteLine($"[manual] {item.DependencyName}: review plan output.");
            return;
        }

        var commandText = item.Steps.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(commandText))
        {
            summary.SkippedByPolicy++;
            _writer.WriteLine($"[skipped by policy] {item.DependencyName}: no executable command was found.");
            return;
        }

        if (item.RequiresAdmin)
        {
            summary.SkippedByPolicy++;
            _writer.WriteLine($"[skipped by policy] {item.DependencyName}: command requires admin privileges.");
            return;
        }

        if (!SafeCommandAllowlist.Contains(commandText))
        {
            summary.SkippedByPolicy++;
            _writer.WriteLine($"[skipped by policy] {item.DependencyName}: command is not allowlisted.");
            return;
        }

        _writer.WriteLine($"[command] {item.DependencyName}");
        _writer.WriteLine($"Command: {commandText}");
        _writer.Write("Run this command? [y/N]: ");

        var response = await _reader.ReadLineAsync(cancellationToken);
        if (!IsAccepted(response))
        {
            summary.SkippedByUser++;
            _writer.WriteLine($"[skipped by user] {item.DependencyName}");
            return;
        }

        var request = CreateProcessRequest(commandText);
        var result = await _processRunner.RunAsync(request, cancellationToken);

        if (result.Succeeded)
        {
            summary.Succeeded++;
            _writer.WriteLine($"[succeeded] {item.DependencyName}");
            WriteTrimmedOutput(result.StandardOutput);
            return;
        }

        summary.Failed++;
        _writer.WriteLine($"[failed] {item.DependencyName}: exit code {result.ExitCode}");
        WriteTrimmedOutput(result.StandardError);
    }

    private void RenderSummary(SetupApplySummary summary)
    {
        _writer.WriteLine();
        _writer.WriteLine(
            $"Apply summary: {summary.Succeeded} succeeded, {summary.Failed} failed, {summary.SkippedByUser} skipped by user, {summary.SkippedByPolicy} skipped by policy, {summary.Manual} manual.");
        if (summary.Manual > 0 || summary.SkippedByPolicy > 0)
        {
            _writer.WriteLine("Manual or skipped steps may still be required before React Native development works.");
        }
    }

    private void WriteTrimmedOutput(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _writer.WriteLine(value.Trim());
        }
    }

    private static ProcessRunRequest CreateProcessRequest(string commandText)
    {
        var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new ProcessRunRequest(parts[0], parts.Skip(1).ToArray());
    }

    private static bool IsAccepted(string? response)
    {
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed class SetupApplySummary
    {
        public int Succeeded { get; set; }

        public int Failed { get; set; }

        public int SkippedByUser { get; set; }

        public int SkippedByPolicy { get; set; }

        public int Manual { get; set; }
    }
}
