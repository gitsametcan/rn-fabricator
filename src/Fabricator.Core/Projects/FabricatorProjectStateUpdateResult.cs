namespace Fabricator.Core.Projects;

public sealed class FabricatorProjectStateUpdateResult
{
    private FabricatorProjectStateUpdateResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static FabricatorProjectStateUpdateResult Success()
    {
        return new FabricatorProjectStateUpdateResult([]);
    }

    public static FabricatorProjectStateUpdateResult Failed(IReadOnlyList<string> errors)
    {
        return new FabricatorProjectStateUpdateResult(errors);
    }
}
