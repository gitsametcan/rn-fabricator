using Fabricator.Core;
using Fabricator.Core.Environment;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.Environment;

public sealed class AppleToolDependencyCheckServiceTests
{
    [Fact]
    public async Task CheckAppleToolsAsyncRunsWatchmanOnlyOnNonMacOSAndWarnsForMacOnlyTools()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "2024.01.01.00\n", string.Empty));
        var platform = new FakeSystemPlatform { IsMacOS = false };
        var service = new DependencyCheckService(runner, platform);

        var summary = await service.CheckAppleToolsAsync();

        Assert.Equal(1, summary.PassedCount);
        Assert.Equal(2, summary.WarningCount);
        Assert.Equal(0, summary.FailedCount);
        Assert.Equal(ExitCodes.Success, summary.ExitCode);
        Assert.Collection(
            runner.Requests,
            request =>
            {
                Assert.Equal("watchman", request.FileName);
                Assert.Equal(["--version"], request.Arguments);
            });
        Assert.Contains(summary.Results, result =>
            result.Name == "Xcode"
            && result.Status == DependencyCheckStatus.Warning
            && result.Message == "Xcode check is only available on macOS.");
        Assert.Contains(summary.Results, result =>
            result.Name == "CocoaPods"
            && result.Status == DependencyCheckStatus.Warning
            && result.Message == "CocoaPods check is only available on macOS.");
    }

    [Fact]
    public async Task CheckAppleToolsAsyncRunsWatchmanXcodeAndCocoaPodsOnMacOS()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "2024.01.01.00\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "Xcode 15.4\nBuild version 15F31d\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "1.15.2\n", string.Empty));
        var platform = new FakeSystemPlatform { IsMacOS = true };
        var service = new DependencyCheckService(runner, platform);

        var summary = await service.CheckAppleToolsAsync();

        Assert.Equal(3, summary.PassedCount);
        Assert.Equal(ExitCodes.Success, summary.ExitCode);
        Assert.Collection(
            runner.Requests,
            request =>
            {
                Assert.Equal("watchman", request.FileName);
                Assert.Equal(["--version"], request.Arguments);
            },
            request =>
            {
                Assert.Equal("xcodebuild", request.FileName);
                Assert.Equal(["-version"], request.Arguments);
            },
            request =>
            {
                Assert.Equal("pod", request.FileName);
                Assert.Equal(["--version"], request.Arguments);
            });
    }

    [Fact]
    public async Task CheckAppleToolsAsyncExtractsAppleToolVersions()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "2024.01.01.00\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "Xcode 15.4\nBuild version 15F31d\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "1.15.2\n", string.Empty));
        var platform = new FakeSystemPlatform { IsMacOS = true };
        var service = new DependencyCheckService(runner, platform);

        var summary = await service.CheckAppleToolsAsync();

        Assert.Collection(
            summary.Results,
            result =>
            {
                Assert.Equal("Watchman", result.Name);
                Assert.Equal("2024.01.01.00", result.DetectedVersion);
            },
            result =>
            {
                Assert.Equal("Xcode", result.Name);
                Assert.Equal("Xcode 15.4", result.DetectedVersion);
            },
            result =>
            {
                Assert.Equal("CocoaPods", result.Name);
                Assert.Equal("1.15.2", result.DetectedVersion);
            });
    }

    [Fact]
    public async Task CheckAppleToolsAsyncMapsMissingWatchmanToWarning()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "watchman failed"));
        runner.Enqueue(new ProcessRunResult(0, "Xcode 15.4\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(0, "1.15.2\n", string.Empty));
        var platform = new FakeSystemPlatform { IsMacOS = true };
        var service = new DependencyCheckService(runner, platform);

        var summary = await service.CheckAppleToolsAsync();

        Assert.Equal(2, summary.PassedCount);
        Assert.Equal(1, summary.WarningCount);
        Assert.False(summary.HasRequiredFailures);
        Assert.Equal(ExitCodes.Success, summary.ExitCode);

        var watchmanResult = summary.Results.Single(result => result.Name == "Watchman");
        Assert.Equal(DependencyCheckStatus.Warning, watchmanResult.Status);
        Assert.Contains("brew install watchman", watchmanResult.RemediationHint);
        Assert.Contains("watchman --version", watchmanResult.RemediationHint);
    }

    [Fact]
    public async Task CheckAppleToolsAsyncMapsMissingMacOnlyToolToFailureOnMacOS()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, "2024.01.01.00\n", string.Empty));
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "xcodebuild failed"));
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "pod failed"));
        var platform = new FakeSystemPlatform { IsMacOS = true };
        var service = new DependencyCheckService(runner, platform);

        var summary = await service.CheckAppleToolsAsync();

        Assert.True(summary.HasRequiredFailures);
        Assert.Equal(ExitCodes.EnvironmentFailure, summary.ExitCode);

        var xcodeResult = summary.Results.Single(result => result.Name == "Xcode");
        Assert.Equal(DependencyCheckStatus.Failed, xcodeResult.Status);
        Assert.Contains("Install Xcode from the App Store.", xcodeResult.RemediationHint);
        Assert.Contains("sudo xcode-select --switch /Applications/Xcode.app", xcodeResult.RemediationHint);

        var cocoaPodsResult = summary.Results.Single(result => result.Name == "CocoaPods");
        Assert.Equal(DependencyCheckStatus.Failed, cocoaPodsResult.Status);
        Assert.Contains("brew install cocoapods", cocoaPodsResult.RemediationHint);
    }

    [Fact]
    public async Task CheckAppleToolsAsyncUsesWindowsWatchmanGuidance()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "watchman failed"));
        var platform = new FakeSystemPlatform { IsWindows = true };
        var service = new DependencyCheckService(runner, platform);

        var summary = await service.CheckAppleToolsAsync();

        var watchmanResult = summary.Results.Single(result => result.Name == "Watchman");
        Assert.Equal(DependencyCheckStatus.Warning, watchmanResult.Status);
        Assert.Contains("optional for React Native on Windows", watchmanResult.RemediationHint);
    }
}
