using Fabricator.Core;
using Fabricator.Core.Setup;

namespace Fabricator.Cli.Setup;

public sealed class SetupPlanCommandHandler
{
    private readonly ISetupPlanService _setupPlanService;
    private readonly SetupPlanRenderer _renderer;

    public SetupPlanCommandHandler(
        ISetupPlanService setupPlanService,
        SetupPlanRenderer renderer)
    {
        _setupPlanService = setupPlanService;
        _renderer = renderer;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var plan = await _setupPlanService.BuildPlanAsync(cancellationToken);
        _renderer.Render(plan);

        return ExitCodes.Success;
    }
}
