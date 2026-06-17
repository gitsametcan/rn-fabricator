using Fabricator.Core.Processes;

namespace Fabricator.Tests.Processes;

public sealed class ProcessRunResultTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(127, false)]
    public void SucceededReflectsExitCode(int exitCode, bool expected)
    {
        var result = new ProcessRunResult(exitCode, "out", "err");

        Assert.Equal(expected, result.Succeeded);
    }
}
