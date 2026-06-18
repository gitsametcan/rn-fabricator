namespace Fabricator.Core.Setup;

public sealed class SetupPlanException : Exception
{
    public SetupPlanException(IEnumerable<string> errors)
        : base(string.Join(System.Environment.NewLine, errors))
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}
