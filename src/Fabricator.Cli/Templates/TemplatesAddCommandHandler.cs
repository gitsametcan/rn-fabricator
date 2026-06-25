using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesAddCommandHandler
{
    private readonly FabricatorTemplateAddService _addService;
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;

    public TemplatesAddCommandHandler(
        FabricatorTemplateAddService addService,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _addService = addService;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string templateId,
        string category,
        string sourceProjectDirectory,
        string catalogSource,
        CancellationToken cancellationToken = default)
    {
        var result = await _addService.AddAsync(
            new FabricatorTemplateAddRequest(
                templateId,
                category,
                sourceProjectDirectory,
                catalogSource),
            cancellationToken);

        if (!result.Succeeded)
        {
            RenderFailure(result);
            return ExitCodes.InvalidInput;
        }

        RenderResult(result);
        return ExitCodes.Success;
    }

    private void RenderFailure(FabricatorTemplateAddResult result)
    {
        _errorWriter.WriteLine("Template add failed.");

        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private void RenderResult(FabricatorTemplateAddResult result)
    {
        _outputWriter.WriteLine($"Template added: {result.TemplateId}");
        _outputWriter.WriteLine($"Catalog: {result.CatalogPath}");
        _outputWriter.WriteLine($"Template directory: {result.TemplateDirectory}");
        _outputWriter.WriteLine($"Manifest: {result.ManifestPath}");
        _outputWriter.WriteLine($"Captured files: {result.CapturedFiles.Count}");

        foreach (var file in result.CapturedFiles)
        {
            _outputWriter.WriteLine($"- {file}");
        }
    }
}
