using Fabricator.Core.Environment;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public static class TemplatesDependencies
{
    public static TemplatesListCommandHandler CreateDefaultListHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesListCommandHandler(
            new TemplateCatalogProvider(),
            new TemplateSourceResolver(new SystemEnvironmentVariables()),
            outputWriter,
            errorWriter);
    }

    public static TemplatesCopyCommandHandler CreateDefaultCopyHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesCopyCommandHandler(
            new TemplateCatalogProvider(),
            new TemplateSourceResolver(new SystemEnvironmentVariables()),
            outputWriter,
            errorWriter);
    }

    public static TemplatesApplyCommandHandler CreateDefaultApplyHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesApplyCommandHandler(
            new TemplateCatalogProvider(),
            new FabricatorTemplateApplyService(),
            new TemplateSourceResolver(new SystemEnvironmentVariables()),
            new FabricatorProjectStateService(),
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

    public static TemplatesAddCommandHandler CreateDefaultAddHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesAddCommandHandler(
            new FabricatorTemplateAddService(),
            outputWriter,
            errorWriter);
    }

    public static TemplatesUpdateCommandHandler CreateDefaultUpdateHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesUpdateCommandHandler(
            new FabricatorTemplateUpdateService(),
            outputWriter,
            errorWriter);
    }

    public static TemplatesInfoCommandHandler CreateDefaultInfoHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesInfoCommandHandler(
            new TemplateCatalogProvider(),
            new TemplateSourceResolver(new SystemEnvironmentVariables()),
            outputWriter,
            errorWriter);
    }

    public static TemplatesValidateCommandHandler CreateDefaultValidateHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesValidateCommandHandler(
            new TemplateCatalogValidationService(),
            new TemplateSourceResolver(new SystemEnvironmentVariables()),
            outputWriter,
            errorWriter);
    }

    public static TemplatesStatusCommandHandler CreateDefaultStatusHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        return new TemplatesStatusCommandHandler(
            new FabricatorProjectStateService(),
            outputWriter,
            errorWriter);
    }
}
