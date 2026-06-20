using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public static class TemplatesDependencies
{
    public static TemplatesListCommandHandler CreateDefaultListHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesListCommandHandler(new TemplateCatalogProvider(), outputWriter, errorWriter);
    }
}
