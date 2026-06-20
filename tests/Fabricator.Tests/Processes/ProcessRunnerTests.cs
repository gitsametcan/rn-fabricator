using Fabricator.Core;
using Fabricator.Core.Processes;
using System.Runtime.InteropServices;

namespace Fabricator.Tests.Processes;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsyncCapturesStandardOutput()
    {
        var runner = new ProcessRunner();
        var request = CreateEchoRequest("rn-fabricator");

        var result = await runner.RunAsync(request);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Equal("rn-fabricator", result.StandardOutput.Trim());
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public async Task RunAsyncStreamsAndCapturesStandardOutput()
    {
        var chunks = new List<string>();
        var runner = new ProcessRunner();
        var request = CreateEchoRequest("rn-fabricator") with
        {
            OnStandardOutput = chunks.Add
        };

        var result = await runner.RunAsync(request);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Equal("rn-fabricator", result.StandardOutput.Trim());
        Assert.Equal(result.StandardOutput, string.Concat(chunks));
    }

    [Fact]
    public async Task RunAsyncReturnsFailureResultWhenExecutableIsMissing()
    {
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(ProcessRunRequest.Create("rn-fabricator-missing-executable"));

        Assert.False(result.Succeeded);
        Assert.Equal(ExitCodes.GeneralFailure, result.ExitCode);
        Assert.NotEmpty(result.StandardError);
    }

    private static ProcessRunRequest CreateEchoRequest(string value)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ProcessRunRequest.Create("cmd", "/c", $"echo {value}");
        }

        return ProcessRunRequest.Create("/bin/sh", "-c", $"printf '%s' {value}");
    }
}
