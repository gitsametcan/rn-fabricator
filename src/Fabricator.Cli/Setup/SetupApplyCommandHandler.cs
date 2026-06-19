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
        "brew install cocoapods",
        "brew install --cask temurin",
        "winget install OpenJS.NodeJS.LTS",
        "winget install Git.Git",
        "winget install EclipseAdoptium.Temurin.17.JDK"
    };

    private readonly ISetupPlanService _setupPlanService;
    private readonly SetupPlanRenderer _planRenderer;
    private readonly SetupExecutionResultRenderer _executionResultRenderer;
    private readonly IProcessRunner _processRunner;
    private readonly TextReader _reader;
    private readonly TextWriter _writer;
    private readonly TextWriter _errorWriter;

    public SetupApplyCommandHandler(
        ISetupPlanService setupPlanService,
        SetupPlanRenderer planRenderer,
        SetupExecutionResultRenderer executionResultRenderer,
        IProcessRunner processRunner,
        TextReader reader,
        TextWriter writer,
        TextWriter? errorWriter = null)
    {
        _setupPlanService = setupPlanService;
        _planRenderer = planRenderer;
        _executionResultRenderer = executionResultRenderer;
        _processRunner = processRunner;
        _reader = reader;
        _writer = writer;
        _errorWriter = errorWriter ?? Console.Error;
    }

    public async Task<int> RunAsync(
        string? profileId = null,
        string? reactNativeVersion = null,
        bool dryRun = false,
        bool yes = false,
        CancellationToken cancellationToken = default)
    {
        if (dryRun && yes)
        {
            _errorWriter.WriteLine("Choose either --dry-run or --yes, not both.");
            return ExitCodes.InvalidInput;
        }

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
        if (dryRun)
        {
            _writer.WriteLine("Mode: dry-run (no commands will be executed).");
        }
        else if (yes)
        {
            _writer.WriteLine("Mode: yes (safe allowlisted commands will run without prompts).");
        }

        var stepResults = new List<SetupExecutionStepResult>();

        foreach (var item in plan.Items)
        {
            stepResults.Add(await ApplyItemAsync(item, dryRun, yes, cancellationToken));
        }

        var executionResult = new SetupExecutionResult(stepResults);
        _executionResultRenderer.RenderSummary(executionResult);

        return executionResult.HasFailures ? ExitCodes.GeneralFailure : ExitCodes.Success;
    }

    private async Task<SetupExecutionStepResult> ApplyItemAsync(
        SetupPlanItem item,
        bool dryRun,
        bool yes,
        CancellationToken cancellationToken)
    {
        if (item.Kind != SetupPlanItemKind.Command)
        {
            return RenderStep(new SetupExecutionStepResult(
                item.DependencyName,
                SetupExecutionStepStatus.ManualOnly));
        }

        var commandText = item.Steps.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return RenderStep(new SetupExecutionStepResult(
                item.DependencyName,
                SetupExecutionStepStatus.SkippedByPolicy,
                Reason: "no executable command was found."));
        }

        if (item.RequiresAdmin)
        {
            return RenderStep(new SetupExecutionStepResult(
                item.DependencyName,
                SetupExecutionStepStatus.SkippedByPolicy,
                CommandText: commandText,
                Reason: "command requires admin privileges."));
        }

        if (!SafeCommandAllowlist.Contains(commandText))
        {
            return RenderStep(new SetupExecutionStepResult(
                item.DependencyName,
                SetupExecutionStepStatus.SkippedByPolicy,
                CommandText: commandText,
                Reason: "command is not allowlisted."));
        }

        if (dryRun)
        {
            return RenderStep(new SetupExecutionStepResult(
                item.DependencyName,
                SetupExecutionStepStatus.WouldRun,
                CommandText: commandText));
        }

        _executionResultRenderer.RenderCommandStart(item.DependencyName, commandText);
        if (!yes)
        {
            _writer.Write("Run this command? [y/N]: ");

            var response = await _reader.ReadLineAsync(cancellationToken);
            if (!IsAccepted(response))
            {
                return RenderStep(new SetupExecutionStepResult(
                    item.DependencyName,
                    SetupExecutionStepStatus.SkippedByUser,
                    CommandText: commandText));
            }
        }

        var request = CreateProcessRequest(commandText);
        var result = await _processRunner.RunAsync(request, cancellationToken);

        if (result.Succeeded)
        {
            return RenderStep(new SetupExecutionStepResult(
                item.DependencyName,
                SetupExecutionStepStatus.Succeeded,
                CommandText: commandText,
                ExitCode: result.ExitCode,
                DiagnosticOutput: result.StandardOutput));
        }

        return RenderStep(new SetupExecutionStepResult(
            item.DependencyName,
            SetupExecutionStepStatus.Failed,
            CommandText: commandText,
            ExitCode: result.ExitCode,
            DiagnosticOutput: result.StandardError));
    }

    private SetupExecutionStepResult RenderStep(SetupExecutionStepResult step)
    {
        _executionResultRenderer.RenderStep(step);
        return step;
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
}
