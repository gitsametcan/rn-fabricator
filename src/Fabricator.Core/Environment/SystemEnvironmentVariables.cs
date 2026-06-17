namespace Fabricator.Core.Environment;

public sealed class SystemEnvironmentVariables : IEnvironmentVariables
{
    public string? Get(string name)
    {
        return System.Environment.GetEnvironmentVariable(name);
    }
}
