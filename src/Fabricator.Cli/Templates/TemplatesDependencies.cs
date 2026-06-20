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

    public static TemplatesCopyCommandHandler CreateDefaultCopyHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesCopyCommandHandler(new TemplateCatalogProvider(), outputWriter, errorWriter);
    }
}
