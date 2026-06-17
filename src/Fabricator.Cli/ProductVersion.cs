using System.Reflection;

namespace Fabricator.Cli;

public static class ProductVersion
{
    public static string Current
    {
        get
        {
            var assembly = typeof(ProductVersion).Assembly;
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            return string.IsNullOrWhiteSpace(informationalVersion)
                ? assembly.GetName().Version?.ToString() ?? "unknown"
                : informationalVersion;
        }
    }
}
