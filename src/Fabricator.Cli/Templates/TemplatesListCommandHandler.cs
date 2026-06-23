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
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            _errorWriter.WriteLine("Template source is required. Pass --source <catalog-url-or-path>.");
            return ExitCodes.InvalidInput;
        }

        if (category is not null && string.IsNullOrWhiteSpace(category))
        {
            _errorWriter.WriteLine("Template category cannot be empty when --category is provided.");
            return ExitCodes.InvalidInput;
        }

        try
        {
            var catalog = await _templateCatalogProvider.ListTemplatesAsync(source, cancellationToken);
            RenderCatalog(source, catalog, category);
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

    private void RenderCatalog(
        string source,
        FabricatorTemplateCatalog catalog,
        string? category)
    {
        _outputWriter.WriteLine("Fabricator templates");
        _outputWriter.WriteLine($"Source: {source}");

        if (!string.IsNullOrWhiteSpace(category))
        {
            _outputWriter.WriteLine($"Category: {category}");
        }

        _outputWriter.WriteLine();

        var templates = FilterTemplates(catalog, category);
        if (templates.Count == 0)
        {
            _outputWriter.WriteLine(string.IsNullOrWhiteSpace(category)
                ? "No templates found."
                : $"No templates found for category: {category}");
            return;
        }

        foreach (var template in templates.OrderBy(template => template.Id, StringComparer.Ordinal))
        {
            _outputWriter.WriteLine($"- {template.Id} ({template.Version})");
            _outputWriter.WriteLine($"  Name: {template.DisplayName}");
            _outputWriter.WriteLine($"  Category: {RenderValue(template.Category)}");
            _outputWriter.WriteLine($"  Description: {template.Description}");
        }
    }

    private static IReadOnlyList<FabricatorTemplateCatalogEntry> FilterTemplates(
        FabricatorTemplateCatalog catalog,
        string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return catalog.Templates;
        }

        return catalog.Templates
            .Where(template => string.Equals(template.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string RenderValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}
