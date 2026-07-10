using Fabricator.Core.Processes;

namespace Fabricator.Core.CommandBridge;

public sealed record FabricatorCommandResult(
    FabricatorCommandRequest Request,
    FabricatorCommandState State,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    FabricatorCommandError? Error)
{
    public bool Succeeded => State == FabricatorCommandState.Completed && ExitCode == ExitCodes.Success;

    public static FabricatorCommandResult FromProcessResult(
        FabricatorCommandRequest request,
        ProcessRunResult processResult)
    {
        var state = processResult.Succeeded
            ? FabricatorCommandState.Completed
            : FabricatorCommandState.Failed;

        var error = processResult.Succeeded
            ? null
            : new FabricatorCommandError(
                FabricatorCommandErrorKind.ProcessFailure,
                "Command failed.",
                processResult.StandardError,
                "Review the command output and retry after addressing the failure.",
                processResult.ExitCode);

        return new FabricatorCommandResult(
            request,
            state,
            processResult.ExitCode,
            processResult.StandardOutput,
            processResult.StandardError,
            error);
    }

    public static FabricatorCommandResult InvalidInput(
        FabricatorCommandRequest request,
        string message,
        string? details = null,
        string? recoveryHint = null)
    {
        return new FabricatorCommandResult(
            request,
            FabricatorCommandState.Failed,
            ExitCodes.InvalidInput,
            string.Empty,
            details ?? string.Empty,
            new FabricatorCommandError(
                FabricatorCommandErrorKind.InvalidInput,
                message,
                details,
                recoveryHint,
                ExitCodes.InvalidInput));
    }

    public static FabricatorCommandResult Canceled(
        FabricatorCommandRequest request,
        string message = "Command was canceled.")
    {
        return new FabricatorCommandResult(
            request,
            FabricatorCommandState.Canceled,
            null,
            string.Empty,
            string.Empty,
            new FabricatorCommandError(
                FabricatorCommandErrorKind.Canceled,
                message));
    }
}
