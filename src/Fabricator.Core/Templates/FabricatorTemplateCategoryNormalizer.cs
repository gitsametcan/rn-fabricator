namespace Fabricator.Core.Templates;

internal static class FabricatorTemplateCategoryNormalizer
{
    public static FabricatorTemplateCategory Normalize(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();

        return normalized switch
        {
            "screen" or "screens" => new FabricatorTemplateCategory("screen", "screens"),
            "component" or "components" => new FabricatorTemplateCategory("component", "components"),
            "service" or "services" => new FabricatorTemplateCategory("service", "services"),
            "util" or "utils" => new FabricatorTemplateCategory("util", "utils"),
            "navigation" => new FabricatorTemplateCategory("navigation", "navigation"),
            _ => new FabricatorTemplateCategory(normalized, normalized)
        };
    }
}

internal sealed record FabricatorTemplateCategory(string CanonicalCategory, string FolderKey);
