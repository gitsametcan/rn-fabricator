using Fabricator.Core;
using Fabricator.Core.CommandBridge;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.CommandBridge;

public sealed class FabricatorCommandBridgeSmokeTests
{
    [Fact]
    public async Task FakeBridgeStreamsOutputAndFinalResultWithoutExternalTools()
    {
        var request = FabricatorCommandRequest.Create("doctor");
        var result = FabricatorCommandResult.FromProcessResult(
            request,
            new ProcessRunResult(ExitCodes.Success, "ok", string.Empty));
        var bridge = new FakeFabricatorCommandBridge(
        [
            FabricatorCommandUpdate.Starting(),
            FabricatorCommandUpdate.Running(1),
            FabricatorCommandUpdate.StandardOutput("checking node", 2),
            FabricatorCommandUpdate.Completed(result, 3)
        ]);

        var updates = new List<FabricatorCommandUpdate>();
        await foreach (var update in bridge.RunAsync(request))
        {
            updates.Add(update);
        }

        Assert.Collection(bridge.Requests, recorded => Assert.Same(request, recorded));
        Assert.Collection(
            updates,
            update => Assert.Equal(FabricatorCommandState.Starting, update.State),
            update => Assert.Equal(FabricatorCommandState.Running, update.State),
            update =>
            {
                Assert.Equal(FabricatorCommandUpdateKind.Output, update.Kind);
                Assert.Equal("checking node", update.Output?.Text);
            },
            update =>
            {
                Assert.Equal(FabricatorCommandUpdateKind.Completed, update.Kind);
                Assert.Same(result, update.Result);
                Assert.True(update.Result?.Succeeded);
            });
    }
}
