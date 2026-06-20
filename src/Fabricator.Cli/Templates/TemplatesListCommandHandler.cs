using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesListCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly ITemplateCatalogProvider _templateCatalogProvider;

    public TemplatesListCommandHandler(
        ITemplateCatalogProvider templateCatalogProvider,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _templateCatalogProvider = templateCatalogProvider;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            _errorWriter.WriteLine("Template source is required. Pass --source <catalog-url-or-path>.");
            return ExitCodes.InvalidInput;
        }

        try
        {
            var catalog = await _templateCatalogProvider.ListTemplatesAsync(source, cancellationToken);
            RenderCatalog(source, catalog);
            return ExitCodes.Success;
        }
        catch (TemplatePackageException exception)
        {
            _errorWriter.WriteLine("Template catalog could not be read.");
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

    private void RenderCatalog(string source, FabricatorTemplateCatalog catalog)
    {
        _outputWriter.WriteLine("Fabricator templates");
        _outputWriter.WriteLine($"Source: {source}");
        _outputWriter.WriteLine();

        if (catalog.Templates.Count == 0)
        {
            _outputWriter.WriteLine("No templates found.");
            return;
        }

        foreach (var template in catalog.Templates.OrderBy(template => template.Id, StringComparer.Ordinal))
        {
            _outputWriter.WriteLine($"- {template.Id} ({template.Version})");
            _outputWriter.WriteLine($"  Name: {template.DisplayName}");
            _outputWriter.WriteLine($"  Description: {template.Description}");
        }
    }
}
