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
                new FabricatorTemplateApplyRequest(package, outputDirectory, overwrite),
                cancellationToken);

            if (result.Succeeded)
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

            RenderResult(sourceResolution.Source, outputDirectory, overwrite, package, result);

            return result.Succeeded ? ExitCodes.Success : ExitCodes.GeneralFailure;
        }
        catch (TemplatePackageException exception)
        {
            _errorWriter.WriteLine("Template could not be read.");
            _errorWriter.WriteLine(exception.Message);
            return ExitCodes.InvalidInput;
        }
        catch (HttpRequestException exception)
        {
            _errorWriter.WriteLine("Template catalog request failed.");
            _errorWriter.WriteLine(exception.Message);
            return ExitCodes.GeneralFailure;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _errorWriter.WriteLine("Template catalog request timed out.");
            _errorWriter.WriteLine(exception.Message);
            return ExitCodes.GeneralFailure;
        }
    }

    private void RenderApplyPreflightFailure(FabricatorProjectStateUpdateResult result)
    {
        _errorWriter.WriteLine("Template apply failed.");

        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private void RenderStateFailure(FabricatorProjectStateUpdateResult result)
    {
        _errorWriter.WriteLine("Template state tracking failed.");

        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private void RenderResult(
        string source,
        string outputDirectory,
        bool overwrite,
        FabricatorTemplatePackage package,
        FabricatorTemplateApplyResult result)
    {
        _outputWriter.WriteLine($"Template applied: {package.Manifest.Id}");
        _outputWriter.WriteLine($"Source: {source}");
        _outputWriter.WriteLine($"Output directory: {Path.GetFullPath(outputDirectory)}");
        _outputWriter.WriteLine($"Overwrite: {(overwrite ? "yes" : "no")}");
        _outputWriter.WriteLine();
        _outputWriter.WriteLine($"Generated: {result.GeneratedFiles.Count}");

        foreach (var file in result.GeneratedFiles)
        {
            _outputWriter.WriteLine($"  + {file}");
        }

        _outputWriter.WriteLine($"Skipped: {result.SkippedFiles.Count}");

        foreach (var file in result.SkippedFiles)
        {
            _outputWriter.WriteLine($"  - {file}");
        }

        _outputWriter.WriteLine($"Exports applied: {result.AppliedExports.Count}");

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
            return;
        }

        _errorWriter.WriteLine("Template apply failed.");
        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }
}
