using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesUpdateCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly FabricatorTemplateUpdateService _updateService;

    public TemplatesUpdateCommandHandler(
        FabricatorTemplateUpdateService updateService,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _updateService = updateService;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string templateId,
        string sourceProjectDirectory,
        string catalogSource,
        CancellationToken cancellationToken = default)
    {
        var result = await _updateService.UpdateAsync(
            new FabricatorTemplateUpdateRequest(
                templateId,
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

    private void RenderFailure(FabricatorTemplateUpdateResult result)
    {
        _errorWriter.WriteLine("Template update failed.");

        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private void RenderResult(FabricatorTemplateUpdateResult result)
    {
        _outputWriter.WriteLine($"Template updated: {result.TemplateId}");
        _outputWriter.WriteLine($"Catalog: {result.CatalogPath}");
        _outputWriter.WriteLine($"Template directory: {result.TemplateDirectory}");
        _outputWriter.WriteLine($"Manifest: {result.ManifestPath}");
        _outputWriter.WriteLine(
            $"Files: {result.AddedFiles.Count} added, {result.ChangedFiles.Count} changed, {result.RemovedFiles.Count} removed, {result.UnchangedFiles.Count} unchanged");
        _outputWriter.WriteLine($"Preserved metadata: {string.Join(", ", result.PreservedMetadata)}");

        RenderFileList("Added", result.AddedFiles);
        RenderFileList("Changed", result.ChangedFiles);
        RenderFileList("Removed", result.RemovedFiles);
    }

    private void RenderFileList(string label, IReadOnlyList<string> files)
    {
        if (files.Count == 0)
        {
            return;
        }

        _outputWriter.WriteLine($"{label}:");

        foreach (var file in files)
        {
            _outputWriter.WriteLine($"- {file}");
        }
    }
}
