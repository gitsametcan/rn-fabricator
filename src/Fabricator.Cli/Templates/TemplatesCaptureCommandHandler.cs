using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesCaptureCommandHandler
{
    private readonly FabricatorTemplateCaptureService _captureService;
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;

    public TemplatesCaptureCommandHandler(
        FabricatorTemplateCaptureService captureService,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _captureService = captureService;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string templateId,
        string category,
        string sourceProjectDirectory,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var result = await _captureService.CaptureAsync(
            new FabricatorTemplateCaptureRequest(
                templateId,
                category,
                sourceProjectDirectory,
                outputDirectory),
            cancellationToken);

        if (!result.Succeeded)
        {
            _errorWriter.WriteLine("Template capture failed.");
            foreach (var error in result.Errors)
            {
                _errorWriter.WriteLine($"- {error}");
            }

            return ExitCodes.InvalidInput;
        }

        _outputWriter.WriteLine($"Template captured: {result.TemplateId}");
        _outputWriter.WriteLine($"Template directory: {result.TemplateDirectory}");
        _outputWriter.WriteLine($"Manifest: {result.ManifestPath}");
        _outputWriter.WriteLine($"Captured files: {result.CapturedFiles.Count}");

        foreach (var file in result.CapturedFiles)
        {
            _outputWriter.WriteLine($"  + {file}");
        }

        return ExitCodes.Success;
    }
}
