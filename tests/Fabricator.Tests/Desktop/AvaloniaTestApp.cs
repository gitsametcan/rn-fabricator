using Avalonia;
using Avalonia.Headless;
using Fabricator.Desktop;

namespace Fabricator.Tests.Desktop;

public static class AvaloniaTestApp
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
