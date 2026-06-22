namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateIntegrationHint(
    string Type,
    string Message,
    string? Target = null);
