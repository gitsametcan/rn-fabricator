using System.Runtime.InteropServices;

namespace Fabricator.Core.Environment;

public sealed class SystemPlatform : ISystemPlatform
{
    public bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
