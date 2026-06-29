namespace Fabricator.Core.Projects;

public sealed record FabricatorAppliedTemplateStatus(
    string Id,
    string Version,
    string Category,
    string Operation,
    string Result,
    DateTimeOffset AppliedAt,
    FabricatorTemplateSourceState Source,
    FabricatorTemplateFileState Files,
    int ExportCount,
    int IntegrationNoteCount,
    string CatalogStatus,
    string? CatalogVersion,
    string? CatalogSource);
