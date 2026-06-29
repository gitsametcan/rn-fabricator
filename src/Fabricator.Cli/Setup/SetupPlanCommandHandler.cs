using Fabricator.Core;
using Fabricator.Core.Setup;

namespace Fabricator.Cli.Setup;

public sealed class SetupPlanCommandHandler
{
    private readonly ISetupPlanService _setupPlanService;
    private readonly SetupPlanRenderer _renderer;
    private readonly TextWriter _errorWriter;

    public SetupPlanCommandHandler(
        ISetupPlanService setupPlanService,
        SetupPlanRenderer renderer,
        TextWriter? errorWriter = null)
    {
        _setupPlanService = setupPlanService;
        _renderer = renderer;
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
            _errorWriter.WriteLine("Invalid setup plan input:");
            foreach (var error in exception.Errors)
            {
                _errorWriter.WriteLine($"- {error}");
            }

            return ExitCodes.InvalidInput;
        }

        _renderer.Render(plan);

        return ExitCodes.Success;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
