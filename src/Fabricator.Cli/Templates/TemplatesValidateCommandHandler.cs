using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesValidateCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly ITemplateCatalogValidationService _validationService;
    private readonly ITemplateSourceResolver _templateSourceResolver;

    public TemplatesValidateCommandHandler(
        ITemplateCatalogValidationService validationService,
        ITemplateSourceResolver templateSourceResolver,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _validationService = validationService;
        _templateSourceResolver = templateSourceResolver;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        var sourceResolution = _templateSourceResolver.Resolve(
            new TemplateSourceResolutionRequest(source, Directory.GetCurrentDirectory()));

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
            var result = await _validationService.ValidateAsync(
                new TemplateCatalogValidationRequest(sourceResolution.Source),
                cancellationToken);

            RenderResult(result);

            return result.Succeeded ? ExitCodes.Success : ExitCodes.InvalidInput;
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

    private void RenderResult(TemplateCatalogValidationResult result)
    {
        _outputWriter.WriteLine("Template catalog validation");
        _outputWriter.WriteLine($"Source: {result.Source}");
        _outputWriter.WriteLine($"Templates: {result.TemplateCount}");
        _outputWriter.WriteLine($"Issues: {result.Issues.Count}");

        if (result.Succeeded)
        {
            _outputWriter.WriteLine("Result: valid");
            _outputWriter.WriteLine("Summary: catalog is valid.");
            return;
        }

        _outputWriter.WriteLine("Result: invalid");
        _outputWriter.WriteLine();

        foreach (var issue in result.Issues)
        {
            var templatePrefix = string.IsNullOrWhiteSpace(issue.TemplateId)
                ? string.Empty
                : $" [{issue.TemplateId}]";
            _outputWriter.WriteLine($"- {issue.Code}{templatePrefix}: {issue.Message}");
        }

        _outputWriter.WriteLine();
        _outputWriter.WriteLine($"Summary: catalog has {result.Issues.Count} issue(s).");
        TemplateCommandOutput.WriteNext(_outputWriter, "Fix the listed catalog issues and run templates validate again.");
    }
}
