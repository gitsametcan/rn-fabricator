using Fabricator.Core.Processes;
using Fabricator.Core.Setup;

namespace Fabricator.Tests.Setup;

public sealed class PackageManagerDetectorTests
{
    [Fact]
    public async Task DetectAsyncUsesHomebrewOnMacOS()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "Homebrew 4.0.0\n", string.Empty));
        var detector = new PackageManagerDetector(
            runner,
            new FakeSystemPlatform { IsMacOS = true });

        var result = await detector.DetectAsync();

        Assert.True(result.IsAvailable);
        Assert.Equal("Homebrew", result.Name);
        Assert.Equal("brew", result.CommandName);
        var request = Assert.Single(runner.Requests);
        Assert.Equal("brew", request.FileName);
        Assert.Equal(["--version"], request.Arguments);
    }

    [Fact]
    public async Task DetectAsyncUsesWingetOnWindows()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "v1.9.0\n", string.Empty));
        var detector = new PackageManagerDetector(
            runner,
            new FakeSystemPlatform { IsWindows = true });

        var result = await detector.DetectAsync();

        Assert.True(result.IsAvailable);
        Assert.Equal("winget", result.Name);
        Assert.Equal("winget", result.CommandName);
    }

    [Fact]
    public async Task DetectAsyncUsesAptGetOnLinux()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "apt 2.8.0\n", string.Empty));
        var detector = new PackageManagerDetector(
            runner,
            new FakeSystemPlatform { IsLinux = true });

        var result = await detector.DetectAsync();

        Assert.True(result.IsAvailable);
        Assert.Equal("apt-get", result.Name);
        Assert.Equal("apt-get", result.CommandName);
    }

    [Fact]
    public async Task DetectAsyncFallsBackToManualGuidanceWhenPackageManagerIsMissing()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "brew missing"));
        var detector = new PackageManagerDetector(
            runner,
            new FakeSystemPlatform { IsMacOS = true });

        var result = await detector.DetectAsync();

        Assert.False(result.IsAvailable);
        Assert.Equal("manual", result.Name);
        Assert.Null(result.CommandName);
    }
}
