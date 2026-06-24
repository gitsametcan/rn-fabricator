namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateIntegrationReport(
    string Kind,
    string Message,
    string? Target = null);
