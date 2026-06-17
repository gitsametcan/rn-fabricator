namespace Fabricator.Core.Environment;

public interface IDependencyCheckService
{
    Task<DependencyCheckSummary> CheckCoreToolsAsync(CancellationToken cancellationToken = default);
}
