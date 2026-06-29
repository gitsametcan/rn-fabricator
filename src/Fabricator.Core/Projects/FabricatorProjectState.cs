namespace Fabricator.Core.Projects;

public sealed record FabricatorProjectState(
    int SchemaVersion,
    string Kind,
    string ToolVersion,
    FabricatorProjectStateMetadata Project,
    IReadOnlyList<FabricatorTemplateSourceState> TemplateSources,
    IReadOnlyList<FabricatorAppliedTemplateState> AppliedTemplates);
