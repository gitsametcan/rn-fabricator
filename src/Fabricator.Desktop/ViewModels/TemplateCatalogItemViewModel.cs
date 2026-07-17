using Fabricator.Core.Templates;

namespace Fabricator.Desktop.ViewModels;

public sealed class TemplateCatalogItemViewModel
{
    public TemplateCatalogItemViewModel(FabricatorTemplateCatalogEntry entry)
    {
        Id = entry.Id;
        DisplayName = entry.DisplayName;
        Description = entry.Description;
        Version = entry.Version;
        Tags = entry.Tags.Count == 0
            ? "No tags"
            : string.Join(", ", entry.Tags);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public string Version { get; }

    public string Tags { get; }
}
