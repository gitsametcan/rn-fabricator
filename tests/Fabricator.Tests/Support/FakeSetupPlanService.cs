using Fabricator.Core.Setup;

namespace Fabricator.Tests;

public sealed class FakeSetupPlanService : ISetupPlanService
{
    public SetupPlan Plan { get; set; } = new(
        "test",
        PackageManagerInfo.NotDetected(),
        []);

    public int CallCount { get; private set; }

    public Task<SetupPlan> BuildPlanAsync(CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(Plan);
    }
}
