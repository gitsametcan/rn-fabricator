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

    public static TemplatesApplyCommandHandler CreateDefaultApplyHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesApplyCommandHandler(
            new TemplateCatalogProvider(),
            new FabricatorTemplateApplyService(),
            outputWriter,
            errorWriter);
    }

    public static TemplatesCaptureCommandHandler CreateDefaultCaptureHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesCaptureCommandHandler(
            new FabricatorTemplateCaptureService(),
            outputWriter,
            errorWriter);
    }

    public static TemplatesInfoCommandHandler CreateDefaultInfoHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesInfoCommandHandler(new TemplateCatalogProvider(), outputWriter, errorWriter);
    }
}
