namespace Fabricator.Core.CommandBridge;

public sealed record FabricatorCommandOutputChunk(
    FabricatorCommandOutputStream Stream,
    string Text,
    int Sequence);
