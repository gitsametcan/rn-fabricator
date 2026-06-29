namespace Fabricator.Core.Setup;

public interface ISetupPlanService
{
    Task<SetupPlan> BuildPlanAsync(
        SetupPlanRequest request,
        CancellationToken cancellationToken = default);
}
