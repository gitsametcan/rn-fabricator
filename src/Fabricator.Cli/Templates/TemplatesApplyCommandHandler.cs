using Fabricator.Core;
using Fabricator.Core.Projects;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesApplyCommandHandler
{
    private readonly FabricatorTemplateApplyService _applyService;
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly IFabricatorProjectStateService _projectStateService;
    private readonly ITemplateCatalogProvider _templateCatalogProvider;
    private readonly ITemplateSourceResolver _templateSourceResolver;

    public TemplatesApplyCommandHandler(
        ITemplateCatalogProvider templateCatalogProvider,
        FabricatorTemplateApplyService applyService,
        ITemplateSourceResolver templateSourceResolver,
        IFabricatorProjectStateService projectStateService,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _templateCatalogProvider = templateCatalogProvider;
        _applyService = applyService;
        _templateSourceResolver = templateSourceResolver;
        _projectStateService = projectStateService;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string templateId,
        string source,
        string outputDirectory,
        bool overwrite,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            _errorWriter.WriteLine("Template id is required.");
            return ExitCodes.InvalidInput;
        }

        var sourceResolution = _templateSourceResolver.Resolve(
            new TemplateSourceResolutionRequest(
                source,
                Directory.GetCurrentDirectory(),
                outputDirectory));

        if (!sourceResolution.Succeeded || string.IsNullOrWhiteSpace(sourceResolution.Source))
        {
            _errorWriter.WriteLine(sourceResolution.ErrorMessage);
            TemplateCommandOutput.WriteNext(
                _errorWriter,
                "Pass --source <catalog-path-or-url> or set RN_FABRICATOR_TEMPLATE_SOURCE, then retry.");
            return ExitCodes.InvalidInput;
        }

        try
        {
            var statePreflight = _projectStateService.ValidateCanTrack(outputDirectory);
            if (!statePreflight.Succeeded)
            {
                RenderApplyPreflightFailure(statePreflight);
                return ExitCodes.GeneralFailure;
            }

            var package = await _templateCatalogProvider.GetTemplateAsync(sourceResolution.Source, templateId, cancellationToken);
            var result = await _applyService.ApplyAsync(
                new FabricatorTemplateApplyRequest(package, outputDirectory, overwrite, dryRun),
                cancellationToken);

            if (result.Succeeded && !dryRun)
            {
                var stateUpdate = await _projectStateService.TrackApplyAsync(
                    new FabricatorTemplateApplyStateTrackingRequest(
                        outputDirectory,
                        sourceResolution.Source,
                        package,
                        result),
                    cancellationToken);

                if (!stateUpdate.Succeeded)
                {
                    RenderStateFailure(stateUpdate);
                    return ExitCodes.GeneralFailure;
                }
            }

            RenderResult(sourceResolution.Source, outputDirectory, overwrite, dryRun, package, result);

            return result.Succeeded ? ExitCodes.Success : ExitCodes.GeneralFailure;
        }
        catch (TemplatePackageException exception)
        {
            _errorWriter.WriteLine("Template could not be read.");
            _errorWriter.WriteLine(exception.Message);
            TemplateCommandOutput.WriteNext(
                _errorWriter,
                $"Check template id '{templateId}' and the catalog source, then run templates apply again.");
            return ExitCodes.InvalidInput;
        }
        catch (HttpRequestException exception)
        {
            _errorWriter.WriteLine("Template catalog request failed.");
            _errorWriter.WriteLine(exception.Message);
            TemplateCommandOutput.WriteNext(_errorWriter, "Check your network connection and catalog URL, then retry.");
            return ExitCodes.GeneralFailure;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _errorWriter.WriteLine("Template catalog request timed out.");
            _errorWriter.WriteLine(exception.Message);
            TemplateCommandOutput.WriteNext(_errorWriter, "Retry the command or use a local catalog source.");
            return ExitCodes.GeneralFailure;
        }
    }

    private void RenderApplyPreflightFailure(FabricatorProjectStateUpdateResult result)
    {
        _errorWriter.WriteLine("Template apply failed.");

        TemplateCommandOutput.WriteErrors(_errorWriter, result.Errors);
        TemplateCommandOutput.WriteNext(_errorWriter, "Run this command from a Fabricator project root or pass --output <project-root>.");
    }

    private void RenderStateFailure(FabricatorProjectStateUpdateResult result)
    {
        _errorWriter.WriteLine("Template state tracking failed.");

        TemplateCommandOutput.WriteErrors(_errorWriter, result.Errors);
        TemplateCommandOutput.WriteNext(_errorWriter, "Fix fabricator.json access or permissions, then retry.");
    }

    private void RenderResult(
        string source,
        string outputDirectory,
        bool overwrite,
        bool dryRun,
        FabricatorTemplatePackage package,
        FabricatorTemplateApplyResult result)
    {
        _outputWriter.WriteLine(dryRun
            ? $"Template apply dry-run: {package.Manifest.Id}"
            : $"Template applied: {package.Manifest.Id}");
        _outputWriter.WriteLine($"Mode: {(dryRun ? "dry-run (no files, exports, or fabricator.json changes will be written)" : "apply")}");
        _outputWriter.WriteLine($"Source: {source}");
        _outputWriter.WriteLine($"Output directory: {Path.GetFullPath(outputDirectory)}");
        _outputWriter.WriteLine($"Overwrite: {TemplateCommandOutput.RenderYesNo(overwrite)}");
        _outputWriter.WriteLine(RenderStateTracking(dryRun, result.Succeeded));
        _outputWriter.WriteLine();
        _outputWriter.WriteLine(dryRun
            ? $"Would generate: {result.GeneratedFiles.Count}"
            : $"Generated: {result.GeneratedFiles.Count}");

        foreach (var file in result.GeneratedFiles)
        {
            _outputWriter.WriteLine($"  + {file}");
        }

        _outputWriter.WriteLine($"Skipped: {result.SkippedFiles.Count}");

        foreach (var file in result.SkippedFiles)
        {
            _outputWriter.WriteLine($"  - {file}");
        }

        _outputWriter.WriteLine(dryRun
            ? $"Exports to apply: {result.AppliedExports.Count}"
            : $"Exports applied: {result.AppliedExports.Count}");

        foreach (var appliedExport in result.AppliedExports)
        {
            _outputWriter.WriteLine($"  + {appliedExport}");
        }

        _outputWriter.WriteLine($"Exports skipped: {result.SkippedExports.Count}");

        foreach (var skippedExport in result.SkippedExports)
        {
            _outputWriter.WriteLine($"  - {skippedExport}");
        }

        _outputWriter.WriteLine($"Integration notes: {result.IntegrationReports.Count}");

        foreach (var report in result.IntegrationReports)
        {
            var target = string.IsNullOrWhiteSpace(report.Target) ? string.Empty : $" ({report.Target})";
            _outputWriter.WriteLine($"  - [{report.Kind}]{target} {report.Message}");
        }

        if (result.Succeeded)
        {
            _outputWriter.WriteLine(
                $"Summary: {result.GeneratedFiles.Count} generated, {result.SkippedFiles.Count} skipped, {result.AppliedExports.Count} export(s) applied, {result.SkippedExports.Count} export(s) skipped.");
            return;
        }

        _errorWriter.WriteLine("Template apply failed.");
        TemplateCommandOutput.WriteErrors(_errorWriter, result.Errors);
        TemplateCommandOutput.WriteNext(_errorWriter, "Fix the listed file, export, or project compatibility issue, then retry.");
        _outputWriter.WriteLine(
            $"Summary: {result.GeneratedFiles.Count} generated, {result.SkippedFiles.Count} skipped, {result.AppliedExports.Count} export(s) applied, {result.Errors.Count} error(s).");
    }

    private static string RenderStateTracking(bool dryRun, bool succeeded)
    {
        if (dryRun)
        {
            return "State tracking: skipped (dry-run)";
        }

        return succeeded
            ? "State tracking: fabricator.json updated"
            : "State tracking: skipped (apply failed)";
    }
}
