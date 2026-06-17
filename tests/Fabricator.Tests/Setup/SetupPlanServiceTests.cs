using Fabricator.Core.Environment;
using Fabricator.Core.Setup;

namespace Fabricator.Tests.Setup;

public sealed class SetupPlanServiceTests
{
    [Fact]
    public async Task BuildPlanAsyncCreatesCommandManualAndEnvironmentItems()
    {
        var dependencyCheckService = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Node.js", null, "Node.js missing.", "Install Node."),
                DependencyCheckResult.Failed("npm", null, "npm missing.", "Install npm.")
            ]),
            AppleToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Xcode", null, "Xcode missing.", "Install Xcode."),
                DependencyCheckResult.Warning("Watchman", null, "Watchman missing.", "Install Watchman.")
            ]),
            AndroidToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Android SDK", null, "Android SDK missing.", "Install Android Studio.")
            ])
        };
        var service = new SetupPlanService(
            dependencyCheckService,
            new FakeSystemPlatform { IsMacOS = true },
            new FixedPackageManagerDetector(new PackageManagerInfo("Homebrew", true, "brew")));

        var plan = await service.BuildPlanAsync();

        Assert.Equal("macOS", plan.PlatformName);
        Assert.Equal("Homebrew", plan.PackageManager.Name);
        Assert.Contains(plan.Items, item =>
            item.DependencyName == "Node.js"
            && item.Kind == SetupPlanItemKind.Command
            && item.Steps.Contains("brew install node"));
        Assert.DoesNotContain(plan.Items, item => item.DependencyName == "npm");
        Assert.Contains(plan.Items, item =>
            item.DependencyName == "Xcode"
            && item.Kind == SetupPlanItemKind.Manual);
        Assert.Contains(plan.Items, item =>
            item.DependencyName == "Android SDK"
            && item.Kind == SetupPlanItemKind.Environment);
        Assert.Equal(1, dependencyCheckService.CoreToolsCallCount);
        Assert.Equal(1, dependencyCheckService.AppleToolsCallCount);
        Assert.Equal(1, dependencyCheckService.AndroidToolsCallCount);
    }

    [Fact]
    public async Task BuildPlanAsyncUsesManualGuidanceWhenPackageManagerIsMissing()
    {
        var dependencyCheckService = new FakeDependencyCheckService
        {
            CoreToolsSummary = new DependencyCheckSummary(
            [
                DependencyCheckResult.Failed("Git", null, "Git missing.", "Install Git.")
            ])
        };
        var service = new SetupPlanService(
            dependencyCheckService,
            new FakeSystemPlatform { IsWindows = true },
            new FixedPackageManagerDetector(PackageManagerInfo.NotDetected()));

        var plan = await service.BuildPlanAsync();

        var gitItem = Assert.Single(plan.Items);
        Assert.Equal("Windows", plan.PlatformName);
        Assert.Equal(SetupPlanItemKind.Manual, gitItem.Kind);
        Assert.Contains(gitItem.Steps, step => step.Contains("https://git-scm.com/downloads", StringComparison.Ordinal));
    }

    private sealed class FixedPackageManagerDetector : IPackageManagerDetector
    {
        private readonly PackageManagerInfo _packageManager;

        public FixedPackageManagerDetector(PackageManagerInfo packageManager)
        {
            _packageManager = packageManager;
        }

        public Task<PackageManagerInfo> DetectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_packageManager);
        }
    }
}
