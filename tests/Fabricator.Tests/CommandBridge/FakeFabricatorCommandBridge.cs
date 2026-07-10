using Fabricator.Core.CommandBridge;

namespace Fabricator.Tests.CommandBridge;

public sealed class FakeFabricatorCommandBridge : IFabricatorCommandBridge
{
    private readonly IReadOnlyList<FabricatorCommandUpdate> _updates;

    public FakeFabricatorCommandBridge(IReadOnlyList<FabricatorCommandUpdate> updates)
    {
        _updates = updates;
    }

    public List<FabricatorCommandRequest> Requests { get; } = [];

    public async IAsyncEnumerable<FabricatorCommandUpdate> RunAsync(
        FabricatorCommandRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        foreach (var update in _updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return update;
        }
    }
}
