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
        Assert.Equal("Install a supported JDK and make sure `java` is available on PATH.", javaResult.RemediationHint);
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
        Assert.Equal("Set ANDROID_HOME or ANDROID_SDK_ROOT to your Android SDK path.", androidSdkResult.RemediationHint);
    }
}
