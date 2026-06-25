using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesRemoveCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly FabricatorTemplateRemoveService _removeService;

    public TemplatesRemoveCommandHandler(
        FabricatorTemplateRemoveService removeService,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _removeService = removeService;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string templateId,
        string catalogSource,
        bool deleteFiles,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var result = await _removeService.RemoveAsync(
            new FabricatorTemplateRemoveRequest(
                templateId,
                catalogSource,
                deleteFiles,
                dryRun),
            cancellationToken);

        if (!result.Succeeded)
        {
            RenderFailure(result);
            return ExitCodes.InvalidInput;
        }

        RenderResult(result, deleteFiles, dryRun);
        return ExitCodes.Success;
    }

    private void RenderFailure(FabricatorTemplateRemoveResult result)
    {
        _errorWriter.WriteLine("Template remove failed.");

        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private void RenderResult(FabricatorTemplateRemoveResult result, bool deleteFiles, bool dryRun)
    {
        _outputWriter.WriteLine(dryRun
            ? $"Template remove dry-run: {result.TemplateId}"
            : $"Template removed: {result.TemplateId}");
        _outputWriter.WriteLine($"Mode: {(dryRun ? "dry-run (no catalog changes or template file deletions will be written)" : "remove")}");
        _outputWriter.WriteLine($"Catalog: {result.CatalogPath}");
        _outputWriter.WriteLine(dryRun
            ? "Catalog entry would be removed: yes"
            : $"Catalog entry removed: {(result.CatalogEntryRemoved ? "yes" : "no")}");
        _outputWriter.WriteLine($"Template directory: {RenderValue(result.TemplateDirectory)}");

        if (deleteFiles)
        {
            _outputWriter.WriteLine(dryRun
                ? "Template files would be deleted: yes"
                : $"Template files deleted: {(result.TemplateFilesDeleted ? "yes" : "no")}");
            return;
        }

        _outputWriter.WriteLine("Template files kept: yes");
        _outputWriter.WriteLine("Pass --delete-files to remove the template folder when it can be resolved safely.");
    }

    private static string RenderValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}
