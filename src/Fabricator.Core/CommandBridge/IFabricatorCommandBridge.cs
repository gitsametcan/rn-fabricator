namespace Fabricator.Core.CommandBridge;

public interface IFabricatorCommandBridge
{
    IAsyncEnumerable<FabricatorCommandUpdate> RunAsync(
        FabricatorCommandRequest request,
        CancellationToken cancellationToken = default);
}
