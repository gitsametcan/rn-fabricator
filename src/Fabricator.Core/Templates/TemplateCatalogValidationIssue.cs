namespace Fabricator.Core.Templates;

public sealed record TemplateCatalogValidationIssue(
    string Code,
    string Message,
    string? TemplateId = null);
