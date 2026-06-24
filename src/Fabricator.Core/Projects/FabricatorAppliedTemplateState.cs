namespace Fabricator.Core.Projects;

public sealed record FabricatorAppliedTemplateState(
    string Id,
    string Version,
    string Category,
    FabricatorTemplateSourceState Source,
    DateTimeOffset AppliedAt,
    string Operation,
    string Result,
    FabricatorTemplateFileState Files,
    IReadOnlyList<FabricatorTemplateExportState> Exports,
    IReadOnlyList<FabricatorTemplateIntegrationNoteState> IntegrationNotes);
