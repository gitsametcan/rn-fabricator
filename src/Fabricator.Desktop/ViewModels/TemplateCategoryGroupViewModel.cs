namespace Fabricator.Desktop.ViewModels;

public sealed class TemplateCategoryGroupViewModel
{
    public TemplateCategoryGroupViewModel(
        string category,
        IReadOnlyList<TemplateCatalogItemViewModel> templates)
    {
        Category = category;
        Templates = templates;
        CountLabel = $"{templates.Count} template(s)";
    }

    public string Category { get; }

    public string CountLabel { get; }

    public IReadOnlyList<TemplateCatalogItemViewModel> Templates { get; }
}
