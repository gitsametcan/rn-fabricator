namespace Fabricator.Core.Environment;

public interface IDependencyCheckService
{
    Task<DependencyCheckSummary> CheckCoreToolsAsync(CancellationToken cancellationToken = default);

    Task<DependencyCheckSummary> CheckAppleToolsAsync(CancellationToken cancellationToken = default);

    Task<DependencyCheckSummary> CheckAndroidToolsAsync(CancellationToken cancellationToken = default);
}
