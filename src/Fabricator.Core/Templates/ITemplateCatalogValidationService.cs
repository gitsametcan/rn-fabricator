namespace Fabricator.Core.Templates;

public interface ITemplateCatalogValidationService
{
    Task<TemplateCatalogValidationResult> ValidateAsync(
        TemplateCatalogValidationRequest request,
        CancellationToken cancellationToken = default);
}
