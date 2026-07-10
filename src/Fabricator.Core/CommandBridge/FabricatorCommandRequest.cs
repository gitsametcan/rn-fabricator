namespace Fabricator.Core.CommandBridge;

public sealed record FabricatorCommandRequest(
    string CommandName,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string?> Options,
    string? WorkingDirectory = null)
{
    public static FabricatorCommandRequest Create(
        string commandName,
        params string[] arguments)
    {
        return new FabricatorCommandRequest(
            commandName,
            arguments,
            new Dictionary<string, string?>());
    }
}
