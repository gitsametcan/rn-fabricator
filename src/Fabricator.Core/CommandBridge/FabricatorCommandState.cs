namespace Fabricator.Core.CommandBridge;

public enum FabricatorCommandState
{
    NotStarted,
    Starting,
    Running,
    Completed,
    Canceled,
    Failed
}
