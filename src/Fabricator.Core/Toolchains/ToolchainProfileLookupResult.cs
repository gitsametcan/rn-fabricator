namespace Fabricator.Core.Toolchains;

public sealed record ToolchainProfileLookupResult(
    ToolchainProfile? Profile,
    IReadOnlyList<string> Errors)
{
    public bool Succeeded => Profile is not null && Errors.Count == 0;

    public static ToolchainProfileLookupResult Found(ToolchainProfile profile)
    {
        return new ToolchainProfileLookupResult(profile, []);
    }

    public static ToolchainProfileLookupResult Failed(params string[] errors)
    {
        return new ToolchainProfileLookupResult(null, errors);
    }
}
