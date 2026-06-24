namespace Fabricator.Core.Projects;

public sealed record FabricatorProjectStateMetadata(
    string Name,
    string Type,
    DateTimeOffset CreatedAt);
