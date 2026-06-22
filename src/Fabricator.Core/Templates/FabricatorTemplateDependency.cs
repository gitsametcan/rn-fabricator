namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateDependency(
    string Type,
    string Name,
    string? Version = null,
    string? Reason = null);
