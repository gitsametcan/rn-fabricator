using Fabricator.Core;
using Fabricator.Core.CommandBridge;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.CommandBridge;

public sealed class FabricatorCommandUpdateTests
{
    [Fact]
    public void OutputUpdatePreservesStreamTextAndSequence()
    {
        var update = FabricatorCommandUpdate.StandardOutput("creating project", 3);

        Assert.Equal(FabricatorCommandUpdateKind.Output, update.Kind);
        Assert.Equal(FabricatorCommandState.Running, update.State);
        Assert.Equal(3, update.Sequence);
        Assert.NotNull(update.Output);
        Assert.Equal(FabricatorCommandOutputStream.StandardOutput, update.Output.Stream);
        Assert.Equal("creating project", update.Output.Text);
        Assert.Equal(3, update.Output.Sequence);
    }

    [Fact]
    public void StandardErrorUpdatePreservesErrorStream()
    {
        var update = FabricatorCommandUpdate.StandardError("warning", 4);

        Assert.Equal(FabricatorCommandOutputStream.StandardError, update.Output?.Stream);
        Assert.Equal("warning", update.Output?.Text);
    }

    [Fact]
    public void CompletedUpdateCarriesFinalResultAndError()
    {
        var request = FabricatorCommandRequest.Create("doctor");
        var result = FabricatorCommandResult.FromProcessResult(
            request,
            new ProcessRunResult(ExitCodes.EnvironmentFailure, string.Empty, "node missing"));

        var update = FabricatorCommandUpdate.Completed(result, 9);

        Assert.Equal(FabricatorCommandUpdateKind.Completed, update.Kind);
        Assert.Equal(FabricatorCommandState.Failed, update.State);
        Assert.Equal(9, update.Sequence);
        Assert.Same(result, update.Result);
        Assert.Same(result.Error, update.Error);
    }

    [Theory]
    [InlineData(FabricatorCommandState.Starting, 0)]
    [InlineData(FabricatorCommandState.Running, 2)]
    public void StateUpdatesPreserveStateAndSequence(
        FabricatorCommandState expectedState,
        int sequence)
    {
        var update = expectedState == FabricatorCommandState.Starting
            ? FabricatorCommandUpdate.Starting(sequence)
            : FabricatorCommandUpdate.Running(sequence);

        Assert.Equal(FabricatorCommandUpdateKind.StateChanged, update.Kind);
        Assert.Equal(expectedState, update.State);
        Assert.Equal(sequence, update.Sequence);
        Assert.Null(update.Output);
        Assert.Null(update.Result);
    }
}
