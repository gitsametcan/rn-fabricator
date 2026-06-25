namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateApplyResult
{
    public FabricatorTemplateApplyResult(
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> skippedFiles,
        IReadOnlyList<string> overwrittenFiles,
        IReadOnlyList<string> appliedExports,
        IReadOnlyList<string> skippedExports,
        IReadOnlyList<FabricatorTemplateIntegrationReport> integrationReports,
        IReadOnlyList<string> errors)
    {
        GeneratedFiles = generatedFiles;
        SkippedFiles = skippedFiles;
        OverwrittenFiles = overwrittenFiles;
        AppliedExports = appliedExports;
        SkippedExports = skippedExports;
        IntegrationReports = integrationReports;
        Errors = errors;
    }

    public IReadOnlyList<string> GeneratedFiles { get; }

    public IReadOnlyList<string> SkippedFiles { get; }

    public IReadOnlyList<string> OverwrittenFiles { get; }

    public IReadOnlyList<string> AppliedExports { get; }

    public IReadOnlyList<string> SkippedExports { get; }

    public IReadOnlyList<FabricatorTemplateIntegrationReport> IntegrationReports { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded => Errors.Count == 0;
}
