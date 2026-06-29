namespace Fabricator.Core.Templates;

public sealed class TemplateCatalogValidationResult
{
    public TemplateCatalogValidationResult(
        string source,
        int templateCount,
        IReadOnlyList<TemplateCatalogValidationIssue> issues)
    {
        Source = source;
        TemplateCount = templateCount;
        Issues = issues;
    }

    public string Source { get; }

    public int TemplateCount { get; }

    public IReadOnlyList<TemplateCatalogValidationIssue> Issues { get; }

    public bool Succeeded => Issues.Count == 0;
}
