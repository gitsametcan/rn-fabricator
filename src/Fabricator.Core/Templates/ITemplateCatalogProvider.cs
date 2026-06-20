namespace Fabricator.Core.Templates;

public interface ITemplateCatalogProvider
{
    Task<FabricatorTemplatePackage> GetTemplateAsync(
        string source,
        string templateId,
        CancellationToken cancellationToken = default);
}
