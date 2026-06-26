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
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var result = await _addService.AddAsync(
            new FabricatorTemplateAddRequest(
                templateId,
                category,
                sourceProjectDirectory,
                catalogSource,
                dryRun),
            cancellationToken);

        if (!result.Succeeded)
        {
            RenderFailure(result);
            return ExitCodes.InvalidInput;
        }

        RenderResult(result, dryRun);
        return ExitCodes.Success;
    }

    private void RenderFailure(FabricatorTemplateAddResult result)
    {
        _errorWriter.WriteLine("Template add failed.");
        TemplateCommandOutput.WriteErrors(_errorWriter, result.Errors);
        TemplateCommandOutput.WriteNext(
            _errorWriter,
            "Check the template id, category, source project path, and catalog source, then retry.");
    }

    private void RenderResult(FabricatorTemplateAddResult result, bool dryRun)
    {
        _outputWriter.WriteLine(dryRun
            ? $"Template add dry-run: {result.TemplateId}"
            : $"Template added: {result.TemplateId}");
        _outputWriter.WriteLine($"Mode: {(dryRun ? "dry-run (no template files or catalog changes will be written)" : "add")}");
        _outputWriter.WriteLine($"Catalog: {result.CatalogPath}");
        _outputWriter.WriteLine($"Template directory: {result.TemplateDirectory}");
        _outputWriter.WriteLine($"Manifest: {result.ManifestPath}");
        _outputWriter.WriteLine(dryRun
            ? $"Files to capture: {result.CapturedFiles.Count}"
            : $"Captured files: {result.CapturedFiles.Count}");
        _outputWriter.WriteLine(dryRun ? "Catalog entry would be added: yes" : "Catalog entry added: yes");

        foreach (var file in result.CapturedFiles)
        {
            _outputWriter.WriteLine($"- {file}");
        }

        _outputWriter.WriteLine(dryRun
            ? $"Summary: would capture {result.CapturedFiles.Count} file(s) and add 1 catalog entry."
            : $"Summary: captured {result.CapturedFiles.Count} file(s) and added 1 catalog entry.");
        TemplateCommandOutput.WriteNext(
            _outputWriter,
            $"Run templates info {result.TemplateId} --source {result.CatalogPath} to review the saved template.");
    }
}
