using Fabricator.Core.Environment;

namespace Fabricator.Tests;

public sealed class FakeSystemPlatform : ISystemPlatform
{
    public bool IsMacOS { get; set; }

    public bool IsWindows { get; set; }

    public bool IsLinux { get; set; }
}
