namespace Fabricator.Core.CommandBridge;

public sealed record FabricatorCommandUpdate(
    FabricatorCommandUpdateKind Kind,
    FabricatorCommandState State,
    int Sequence,
    FabricatorCommandOutputChunk? Output = null,
    FabricatorCommandResult? Result = null,
    FabricatorCommandError? Error = null)
{
    public static FabricatorCommandUpdate Starting(int sequence = 0)
    {
        return new FabricatorCommandUpdate(
            FabricatorCommandUpdateKind.StateChanged,
            FabricatorCommandState.Starting,
            sequence);
    }

    public static FabricatorCommandUpdate Running(int sequence)
    {
        return new FabricatorCommandUpdate(
            FabricatorCommandUpdateKind.StateChanged,
            FabricatorCommandState.Running,
            sequence);
    }

    public static FabricatorCommandUpdate StandardOutput(string text, int sequence)
    {
        return CreateOutput(FabricatorCommandOutputStream.StandardOutput, text, sequence);
    }

    public static FabricatorCommandUpdate StandardError(string text, int sequence)
    {
        return CreateOutput(FabricatorCommandOutputStream.StandardError, text, sequence);
    }

    public static FabricatorCommandUpdate Completed(FabricatorCommandResult result, int sequence)
    {
        return new FabricatorCommandUpdate(
            FabricatorCommandUpdateKind.Completed,
            result.State,
            sequence,
            Result: result,
            Error: result.Error);
    }

    private static FabricatorCommandUpdate CreateOutput(
        FabricatorCommandOutputStream stream,
        string text,
        int sequence)
    {
        return new FabricatorCommandUpdate(
            FabricatorCommandUpdateKind.Output,
            FabricatorCommandState.Running,
            sequence,
            Output: new FabricatorCommandOutputChunk(stream, text, sequence));
    }
}
