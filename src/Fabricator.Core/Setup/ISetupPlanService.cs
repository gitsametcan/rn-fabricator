namespace Fabricator.Core.Setup;

public interface ISetupPlanService
{
    Task<SetupPlan> BuildPlanAsync(CancellationToken cancellationToken = default);
}
