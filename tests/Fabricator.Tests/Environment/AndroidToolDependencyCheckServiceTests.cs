using Fabricator.Core;
using Fabricator.Core.Environment;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.Environment;

public sealed class AndroidToolDependencyCheckServiceTests
{
    [Fact]
    public async Task CheckAndroidToolsAsyncRunsJavaVersionCommand()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\n"));
        var environmentVariables = new FakeEnvironmentVariables();
        environmentVariables.Set("ANDROID_HOME", "/Users/dev/Library/Android/sdk");
        var service = new DependencyCheckService(
            runner,
            new FakeSystemPlatform(),
            environmentVariables);

        var summary = await service.CheckAndroidToolsAsync();

        Assert.Equal(2, summary.PassedCount);
        Assert.Equal(ExitCodes.Success, summary.ExitCode);
        Assert.Collection(
            runner.Requests,
            request =>
            {
                Assert.Equal("java", request.FileName);
                Assert.Equal(["-version"], request.Arguments);
            });
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncExtractsJavaVersionFromStandardError()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\nOpenJDK Runtime Environment\n"));
        var environmentVariables = new FakeEnvironmentVariables();
        environmentVariables.Set("ANDROID_HOME", "/Users/dev/Library/Android/sdk");
        var service = new DependencyCheckService(
            runner,
            new FakeSystemPlatform(),
            environmentVariables);

        var summary = await service.CheckAndroidToolsAsync();

        var javaResult = summary.Results.Single(result => result.Name == "Java");
        Assert.Equal(DependencyCheckStatus.Passed, javaResult.Status);
        Assert.Equal("openjdk version \"17.0.12\"", javaResult.DetectedVersion);
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncUsesAndroidHomeWhenConfigured()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\n"));
        var environmentVariables = new FakeEnvironmentVariables();
        environmentVariables.Set("ANDROID_HOME", "/Users/dev/Library/Android/sdk");
        var service = new DependencyCheckService(
            runner,
            new FakeSystemPlatform(),
            environmentVariables);

        var summary = await service.CheckAndroidToolsAsync();

        var androidSdkResult = summary.Results.Single(result => result.Name == "Android SDK");
        Assert.Equal(DependencyCheckStatus.Passed, androidSdkResult.Status);
        Assert.Equal("/Users/dev/Library/Android/sdk", androidSdkResult.DetectedVersion);
        Assert.Equal("ANDROID_HOME is configured.", androidSdkResult.Message);
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncFallsBackToAndroidSdkRoot()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\n"));
        var environmentVariables = new FakeEnvironmentVariables();
        environmentVariables.Set("ANDROID_SDK_ROOT", "/opt/android-sdk");
        var service = new DependencyCheckService(
            runner,
            new FakeSystemPlatform(),
            environmentVariables);

        var summary = await service.CheckAndroidToolsAsync();

        var androidSdkResult = summary.Results.Single(result => result.Name == "Android SDK");
        Assert.Equal(DependencyCheckStatus.Passed, androidSdkResult.Status);
        Assert.Equal("/opt/android-sdk", androidSdkResult.DetectedVersion);
        Assert.Equal("ANDROID_SDK_ROOT is configured.", androidSdkResult.Message);
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncFailsWhenJavaIsMissing()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(1, string.Empty, "java failed"));
        var environmentVariables = new FakeEnvironmentVariables();
        environmentVariables.Set("ANDROID_HOME", "/Users/dev/Library/Android/sdk");
        var service = new DependencyCheckService(
            runner,
            new FakeSystemPlatform(),
            environmentVariables);

        var summary = await service.CheckAndroidToolsAsync();

        Assert.True(summary.HasRequiredFailures);
        Assert.Equal(ExitCodes.EnvironmentFailure, summary.ExitCode);

        var javaResult = summary.Results.Single(result => result.Name == "Java");
        Assert.Equal(DependencyCheckStatus.Failed, javaResult.Status);
        Assert.Contains("supported JDK", javaResult.RemediationHint);
        Assert.Contains("java -version", javaResult.RemediationHint);
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncFailsWhenAndroidSdkEnvironmentVariablesAreMissing()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\n"));
        var service = new DependencyCheckService(
            runner,
            new FakeSystemPlatform(),
            new FakeEnvironmentVariables());

        var summary = await service.CheckAndroidToolsAsync();

        Assert.True(summary.HasRequiredFailures);
        Assert.Equal(ExitCodes.EnvironmentFailure, summary.ExitCode);

        var androidSdkResult = summary.Results.Single(result => result.Name == "Android SDK");
        Assert.Equal(DependencyCheckStatus.Failed, androidSdkResult.Status);
        Assert.Equal("Android SDK environment variables were not found.", androidSdkResult.Message);
        Assert.Contains("Install Android Studio", androidSdkResult.RemediationHint);
        Assert.Contains("ANDROID_HOME", androidSdkResult.RemediationHint);
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncUsesMacOSAndroidSdkGuidance()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\n"));
        var platform = new FakeSystemPlatform { IsMacOS = true };
        var service = new DependencyCheckService(
            runner,
            platform,
            new FakeEnvironmentVariables());

        var summary = await service.CheckAndroidToolsAsync();

        var androidSdkResult = summary.Results.Single(result => result.Name == "Android SDK");
        Assert.Contains("More Actions > SDK Manager", androidSdkResult.RemediationHint);
        Assert.Contains("Android Studio > Settings > Languages & Frameworks > Android SDK", androidSdkResult.RemediationHint);
        Assert.Contains("nano ~/.zshrc", androidSdkResult.RemediationHint);
        Assert.Contains("$HOME/Library/Android/sdk", androidSdkResult.RemediationHint);
        Assert.Contains("source ~/.zshrc", androidSdkResult.RemediationHint);
        Assert.Contains("adb --version", androidSdkResult.RemediationHint);
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncUsesWindowsAndroidSdkGuidance()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\n"));
        var platform = new FakeSystemPlatform { IsWindows = true };
        var service = new DependencyCheckService(
            runner,
            platform,
            new FakeEnvironmentVariables());

        var summary = await service.CheckAndroidToolsAsync();

        var androidSdkResult = summary.Results.Single(result => result.Name == "Android SDK");
        Assert.Contains("%LOCALAPPDATA%\\Android\\Sdk", androidSdkResult.RemediationHint);
        Assert.Contains("echo $env:ANDROID_HOME", androidSdkResult.RemediationHint);
    }

    [Fact]
    public async Task CheckAndroidToolsAsyncUsesLinuxAndroidSdkGuidance()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(new ProcessRunResult(0, string.Empty, "openjdk version \"17.0.12\"\n"));
        var platform = new FakeSystemPlatform { IsLinux = true };
        var service = new DependencyCheckService(
            runner,
            platform,
            new FakeEnvironmentVariables());

        var summary = await service.CheckAndroidToolsAsync();

        var androidSdkResult = summary.Results.Single(result => result.Name == "Android SDK");
        Assert.Contains("$HOME/Android/Sdk", androidSdkResult.RemediationHint);
        Assert.Contains("$ANDROID_HOME/platform-tools", androidSdkResult.RemediationHint);
    }
}
