namespace Fabricator.Core.CommandBridge;

public sealed record FabricatorCommandError(
    FabricatorCommandErrorKind Kind,
    string Message,
    string? Details = null,
    string? RecoveryHint = null,
    int? ExitCode = null);
