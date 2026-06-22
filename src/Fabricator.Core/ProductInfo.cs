using System.Reflection;

namespace Fabricator.Core;

public static class ProductInfo
{
    public const string Name = "rn-fabricator";
    public const string Description = "React Native CLI project scaffolding and diagnostics tool.";

    public static string Version =>
        typeof(ProductInfo)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";
}
