namespace Fabricator.Core.Templates;

public interface ITemplatePackageProvider
{
    TemplatePackage GetTemplate(string templateId);
}
