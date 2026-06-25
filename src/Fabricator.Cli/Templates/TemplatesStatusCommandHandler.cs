using Fabricator.Core;
using Fabricator.Core.Projects;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesStatusCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly IFabricatorProjectStateService _projectStateService;

    public TemplatesStatusCommandHandler(
        IFabricatorProjectStateService projectStateService,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _projectStateService = projectStateService;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string projectDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            _errorWriter.WriteLine("Project directory is required.");
            return ExitCodes.InvalidInput;
        }

        var result = await _projectStateService.GetTemplateStatusAsync(projectDirectory, cancellationToken);
        if (!result.Succeeded)
        {
            RenderFailure(result);
            return ExitCodes.GeneralFailure;
        }

        RenderStatus(result);
        return ExitCodes.Success;
    }

    private void RenderFailure(FabricatorProjectTemplateStatusResult result)
    {
        _errorWriter.WriteLine("Template status failed.");

        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private void RenderStatus(FabricatorProjectTemplateStatusResult result)
    {
        _outputWriter.WriteLine("Fabricator template status");
        _outputWriter.WriteLine($"Project: {result.ProjectDirectory}");
        _outputWriter.WriteLine($"Applied templates: {result.AppliedTemplates.Count}");
        _outputWriter.WriteLine();

        if (result.AppliedTemplates.Count == 0)
        {
            _outputWriter.WriteLine("No templates have been applied.");
            return;
        }

        foreach (var template in result.AppliedTemplates.OrderByDescending(template => template.AppliedAt))
        {
            _outputWriter.WriteLine($"- {template.Id} ({template.Version})");
            _outputWriter.WriteLine($"  Category: {RenderValue(template.Category)}");
            _outputWriter.WriteLine($"  Operation: {template.Operation}");
            _outputWriter.WriteLine($"  Result: {template.Result}");
            _outputWriter.WriteLine($"  Applied at: {template.AppliedAt:u}");
            _outputWriter.WriteLine($"  Source: {RenderSource(template.Source)}");
            _outputWriter.WriteLine(
                $"  Files: {template.Files.Written.Count} written, {template.Files.Skipped.Count} skipped, {template.Files.Overwritten.Count} overwritten");
            _outputWriter.WriteLine($"  Exports: {template.ExportCount}");
            _outputWriter.WriteLine($"  Integration notes: {template.IntegrationNoteCount}");
            _outputWriter.WriteLine($"  Catalog: {RenderCatalogStatus(template)}");
        }
    }

    private static string RenderSource(FabricatorTemplateSourceState source)
    {
        return $"{source.Type}:{RenderValue(source.Value)}";
    }

    private static string RenderCatalogStatus(FabricatorAppliedTemplateStatus template)
    {
        return template.CatalogStatus switch
        {
            "current" => $"current ({template.CatalogSource})",
            "catalog-version-differs" => $"catalog version {template.CatalogVersion} ({template.CatalogSource})",
            "not-in-catalog" => $"not found in catalog ({template.CatalogSource})",
            "catalog-missing" => $"catalog missing ({RenderValue(template.CatalogSource)})",
            "catalog-unreadable" => $"catalog unreadable ({template.CatalogSource})",
            "not-checked" => "not checked for embedded or remote source",
            _ => template.CatalogStatus
        };
    }

    private static string RenderValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}
