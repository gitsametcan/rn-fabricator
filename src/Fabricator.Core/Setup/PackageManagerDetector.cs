using Fabricator.Core.Environment;
using Fabricator.Core.Processes;

namespace Fabricator.Core.Setup;

public sealed class PackageManagerDetector : IPackageManagerDetector
{
    private readonly IProcessRunner _processRunner;
    private readonly ISystemPlatform _systemPlatform;

    public PackageManagerDetector(IProcessRunner processRunner, ISystemPlatform systemPlatform)
    {
        _processRunner = processRunner;
        _systemPlatform = systemPlatform;
    }

    public async Task<PackageManagerInfo> DetectAsync(CancellationToken cancellationToken = default)
    {
        if (_systemPlatform.IsMacOS)
        {
            return await DetectCommandAsync("Homebrew", "brew", cancellationToken);
        }

        if (_systemPlatform.IsWindows)
        {
            return await DetectCommandAsync("winget", "winget", cancellationToken);
        }

        if (_systemPlatform.IsLinux)
        {
            return await DetectCommandAsync("apt-get", "apt-get", cancellationToken);
        }

        return PackageManagerInfo.NotDetected();
    }

    private async Task<PackageManagerInfo> DetectCommandAsync(
        string name,
        string commandName,
        CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync(
            new ProcessRunRequest(commandName, ["--version"]),
            cancellationToken);

        return result.Succeeded
            ? new PackageManagerInfo(name, true, commandName)
            : PackageManagerInfo.NotDetected();
    }
}
