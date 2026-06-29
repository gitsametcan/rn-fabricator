namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateExport(
    string IntegrationPoint,
    string Statement,
    string? Source = null);
