using Fabricator.Core.Environment;

namespace Fabricator.Tests;

public sealed class FakeDependencyCheckService : IDependencyCheckService
{
    public DependencyCheckSummary CoreToolsSummary { get; set; } = new([]);

    public DependencyCheckSummary AppleToolsSummary { get; set; } = new([]);

    public DependencyCheckSummary AndroidToolsSummary { get; set; } = new([]);

    public int CoreToolsCallCount { get; private set; }

    public int AppleToolsCallCount { get; private set; }

    public int AndroidToolsCallCount { get; private set; }

    public Task<DependencyCheckSummary> CheckCoreToolsAsync(CancellationToken cancellationToken = default)
    {
        CoreToolsCallCount++;
        return Task.FromResult(CoreToolsSummary);
    }

    public Task<DependencyCheckSummary> CheckAppleToolsAsync(CancellationToken cancellationToken = default)
    {
        AppleToolsCallCount++;
        return Task.FromResult(AppleToolsSummary);
    }

    public Task<DependencyCheckSummary> CheckAndroidToolsAsync(CancellationToken cancellationToken = default)
    {
        AndroidToolsCallCount++;
        return Task.FromResult(AndroidToolsSummary);
    }
}
