namespace Fabricator.Core.Projects;

public sealed record FabricatorTemplateExportState(
    string IntegrationPoint,
    string Path,
    string Statement,
    string Result);
