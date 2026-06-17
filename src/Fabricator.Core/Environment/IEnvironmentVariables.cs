namespace Fabricator.Core.Environment;

public interface IEnvironmentVariables
{
    string? Get(string name);
}
