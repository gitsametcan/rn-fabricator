namespace Fabricator.Core.Templates;

public interface ITemplateSourceResolver
{
    TemplateSourceResolutionResult Resolve(TemplateSourceResolutionRequest request);
}
