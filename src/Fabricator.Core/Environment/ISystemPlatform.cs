namespace Fabricator.Core.Environment;

public interface ISystemPlatform
{
    bool IsMacOS { get; }

    bool IsWindows { get; }

    bool IsLinux { get; }
}
