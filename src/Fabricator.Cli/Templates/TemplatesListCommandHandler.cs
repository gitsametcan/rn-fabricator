using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesListCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly ITemplateCatalogProvider _templateCatalogProvider;
    private readonly ITemplateSourceResolver _templateSourceResolver;

    public TemplatesListCommandHandler(
        ITemplateCatalogProvider templateCatalogProvider,
        ITemplateSourceResolver templateSourceResolver,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _templateCatalogProvider = templateCatalogProvider;
        _templateSourceResolver = templateSourceResolver;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string source,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (category is not null && string.IsNullOrWhiteSpace(category))
        {
            _errorWriter.WriteLine("Template category cannot be empty when --category is provided.");
            return ExitCodes.InvalidInput;
        }

        var sourceResolution = _templateSourceResolver.Resolve(
            new TemplateSourceResolutionRequest(source, Directory.GetCurrentDirectory()));

        if (!sourceResolution.Succeeded || string.IsNullOrWhiteSpace(sourceResolution.Source))
        {
            _errorWriter.WriteLine(sourceResolution.ErrorMessage);
            return ExitCodes.InvalidInput;
        }

        try
        {
            var catalog = await _templateCatalogProvider.ListTemplatesAsync(sourceResolution.Source, cancellationToken);
            RenderCatalog(sourceResolution.Source, catalog, category);
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
