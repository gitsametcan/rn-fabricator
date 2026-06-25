namespace Fabricator.Core.Templates;

public sealed record FabricatorTemplateCaptureRequest(
    string TemplateId,
    string Category,
    string SourceProjectDirectory,
    string OutputDirectory,
    bool DryRun = false);
