using Fabricator.Core.CommandBridge;

namespace Fabricator.Tests.CommandBridge;

public sealed class FabricatorCommandRequestTests
{
    [Fact]
    public void CreatePreservesCommandNameAndArguments()
    {
        var request = FabricatorCommandRequest.Create("doctor", "--json");

        Assert.Equal("doctor", request.CommandName);
        Assert.Equal(["--json"], request.Arguments);
        Assert.Empty(request.Options);
        Assert.Null(request.WorkingDirectory);
    }

    [Fact]
    public void RequestPreservesOptionsAndWorkingDirectory()
    {
        var request = new FabricatorCommandRequest(
            "templates",
            ["apply", "screen/settings-screen"],
            new Dictionary<string, string?>
            {
                ["source"] = "/tmp/catalog",
                ["dry-run"] = null
            },
            "/tmp/app");

        Assert.Equal("templates", request.CommandName);
        Assert.Equal(["apply", "screen/settings-screen"], request.Arguments);
        Assert.Equal("/tmp/catalog", request.Options["source"]);
        Assert.Null(request.Options["dry-run"]);
        Assert.Equal("/tmp/app", request.WorkingDirectory);
    }
}
