namespace Fabricator.Core.Toolchains;

public sealed record ToolchainProfileValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static ToolchainProfileValidationResult Success { get; } = new([]);

    public static ToolchainProfileValidationResult Failed(IEnumerable<string> errors)
    {
        return new ToolchainProfileValidationResult(errors.ToArray());
    }
}
