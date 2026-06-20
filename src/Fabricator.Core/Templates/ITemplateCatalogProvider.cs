namespace Fabricator.Core.Templates;

public interface ITemplateCatalogProvider
{
    Task<FabricatorTemplateCatalog> ListTemplatesAsync(
        string source,
        CancellationToken cancellationToken = default);

    Task<FabricatorTemplatePackage> GetTemplateAsync(
        string source,
        string templateId,
        CancellationToken cancellationToken = default);
}
