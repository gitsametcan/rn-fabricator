namespace Fabricator.Core.Projects;

public sealed record FabricatorTemplateSourceState(
    string Name,
    string Type,
    string Value,
    bool IsDefault);
