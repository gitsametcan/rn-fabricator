using Fabricator.Core.Environment;

namespace Fabricator.Tests;

public sealed class FakeSystemPlatform : ISystemPlatform
{
    public bool IsMacOS { get; set; }
}
