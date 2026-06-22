namespace Fabricator.Core.Projects;

public sealed record FabricatorProjectManifest(
    int SchemaVersion,
    string Kind,
    string FabricatorVersion,
    string ProjectType,
    string SourceRoot,
    IReadOnlyList<FabricatorProjectFolder> Folders,
    IReadOnlyList<FabricatorProjectIntegrationPoint> IntegrationPoints);
