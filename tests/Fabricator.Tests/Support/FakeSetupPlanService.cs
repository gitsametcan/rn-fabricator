using Fabricator.Core.Setup;

namespace Fabricator.Tests;

public sealed class FakeSetupPlanService : ISetupPlanService
{
    public SetupPlan Plan { get; set; } = new(
        "test",
        PackageManagerInfo.NotDetected(),
        []);

    public int CallCount { get; private set; }

    public SetupPlanRequest? LastRequest { get; private set; }

    public SetupPlanException? ExceptionToThrow { get; set; }

    public Task<SetupPlan> BuildPlanAsync(
        SetupPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastRequest = request;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(Plan);
    }
}
