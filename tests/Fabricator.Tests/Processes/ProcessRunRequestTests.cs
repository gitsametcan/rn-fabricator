using Fabricator.Core.Processes;

namespace Fabricator.Tests.Processes;

public sealed class ProcessRunRequestTests
{
    [Fact]
    public void CreateBuildsRequestWithArguments()
    {
        var request = ProcessRunRequest.Create("node", "--version");

        Assert.Equal("node", request.FileName);
        Assert.Equal(["--version"], request.Arguments);
        Assert.Null(request.WorkingDirectory);
    }
}
