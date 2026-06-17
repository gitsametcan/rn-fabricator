using Fabricator.Core.Environment;

namespace Fabricator.Tests;

public sealed class FakeEnvironmentVariables : IEnvironmentVariables
{
    private readonly Dictionary<string, string?> _variables = new(StringComparer.Ordinal);

    public string? Get(string name)
    {
        return _variables.GetValueOrDefault(name);
    }

    public void Set(string name, string? value)
    {
        _variables[name] = value;
    }
}
