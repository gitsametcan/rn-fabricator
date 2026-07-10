namespace Fabricator.Core.CommandBridge;

public enum FabricatorCommandErrorKind
{
    InvalidInput,
    EnvironmentFailure,
    ProcessFailure,
    Canceled,
    Unexpected
}
