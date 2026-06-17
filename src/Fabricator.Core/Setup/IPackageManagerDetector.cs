namespace Fabricator.Core.Setup;

public interface IPackageManagerDetector
{
    Task<PackageManagerInfo> DetectAsync(CancellationToken cancellationToken = default);
}
