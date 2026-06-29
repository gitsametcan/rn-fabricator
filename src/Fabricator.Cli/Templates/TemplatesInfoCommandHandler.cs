using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesInfoCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly ITemplateCatalogProvider _templateCatalogProvider;
    private readonly ITemplateSourceResolver _templateSourceResolver;

    public TemplatesInfoCommandHandler(
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
        string templateId,
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            _errorWriter.WriteLine("Template id is required.");
            return ExitCodes.InvalidInput;
        }

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
            var package = await _templateCatalogProvider.GetTemplateAsync(sourceResolution.Source, templateId, cancellationToken);
            RenderTemplate(sourceResolution.Source, package);

            return ExitCodes.Success;
        }
        catch (TemplatePackageException exception)
        {
            _errorWriter.WriteLine("Template could not be read.");
            _errorWriter.WriteLine(exception.Message);
            TemplateCommandOutput.WriteNext(
                _errorWriter,
                $"Check template id '{templateId}' and the catalog source, then run templates info again.");
            return ExitCodes.InvalidInput;
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

    private void RenderTemplate(string source, FabricatorTemplatePackage package)
    {
        var manifest = package.Manifest;

        _outputWriter.WriteLine($"Template: {manifest.Id} ({manifest.Version})");
        _outputWriter.WriteLine($"Source: {source}");
        _outputWriter.WriteLine($"Name: {manifest.DisplayName}");
        _outputWriter.WriteLine($"Description: {manifest.Description}");
        _outputWriter.WriteLine($"Mode: {manifest.Mode}");
        _outputWriter.WriteLine($"Schema: {manifest.SchemaVersion}");
        _outputWriter.WriteLine($"Category: {RenderValue(manifest.Category)}");
        _outputWriter.WriteLine($"Tags: {RenderList(manifest.Tags)}");
        _outputWriter.WriteLine();

        RenderFiles(manifest.Files);
        RenderDependencies(manifest.Dependencies);
        RenderExports(manifest.Exports);
        RenderIntegrationHints(manifest.IntegrationHints);

        _outputWriter.WriteLine();
        _outputWriter.WriteLine(
            $"Summary: {manifest.Files.Count} file(s), {manifest.Exports?.Count ?? 0} export(s), {manifest.IntegrationHints?.Count ?? 0} integration hint(s).");
        TemplateCommandOutput.WriteNext(
            _outputWriter,
            $"Run templates apply {manifest.Id} --source <catalog> --output <project> to apply this template.");
    }

    private void RenderFiles(IReadOnlyList<FabricatorTemplateFile> files)
    {
        _outputWriter.WriteLine($"Files: {files.Count}");

        foreach (var file in files)
        {
            _outputWriter.WriteLine($"  - {file.Path}");
            _outputWriter.WriteLine($"    Type: {file.Type}");

            if (!string.IsNullOrWhiteSpace(file.TargetPath))
            {
                _outputWriter.WriteLine($"    Target path: {file.TargetPath}");
            }

            if (!string.IsNullOrWhiteSpace(file.TargetFolder))
            {
                _outputWriter.WriteLine($"    Target folder: {file.TargetFolder}");
            }

            if (!string.IsNullOrWhiteSpace(file.Description))
            {
                _outputWriter.WriteLine($"    Description: {file.Description}");
            }
        }

        _outputWriter.WriteLine();
    }

    private void RenderDependencies(IReadOnlyList<FabricatorTemplateDependency>? dependencies)
    {
        _outputWriter.WriteLine($"Dependencies: {dependencies?.Count ?? 0}");

        if (dependencies is null || dependencies.Count == 0)
        {
            _outputWriter.WriteLine();
            return;
        }

        foreach (var dependency in dependencies)
        {
            _outputWriter.WriteLine($"  - {dependency.Type}: {dependency.Name}");

            if (!string.IsNullOrWhiteSpace(dependency.Version))
            {
                _outputWriter.WriteLine($"    Version: {dependency.Version}");
            }

            if (!string.IsNullOrWhiteSpace(dependency.Reason))
            {
                _outputWriter.WriteLine($"    Reason: {dependency.Reason}");
            }
        }

        _outputWriter.WriteLine();
    }

    private void RenderExports(IReadOnlyList<FabricatorTemplateExport>? exports)
    {
        _outputWriter.WriteLine($"Exports: {exports?.Count ?? 0}");

        if (exports is null || exports.Count == 0)
        {
            _outputWriter.WriteLine();
            return;
        }

        foreach (var export in exports)
        {
            _outputWriter.WriteLine($"  - {export.IntegrationPoint}: {export.Statement}");

            if (!string.IsNullOrWhiteSpace(export.Source))
            {
                _outputWriter.WriteLine($"    Source: {export.Source}");
            }
        }

        _outputWriter.WriteLine();
    }

    private void RenderIntegrationHints(IReadOnlyList<FabricatorTemplateIntegrationHint>? integrationHints)
    {
        _outputWriter.WriteLine($"Integration hints: {integrationHints?.Count ?? 0}");

        if (integrationHints is null || integrationHints.Count == 0)
        {
            return;
        }

        foreach (var hint in integrationHints)
        {
            _outputWriter.WriteLine($"  - {hint.Type}: {hint.Message}");

            if (!string.IsNullOrWhiteSpace(hint.Target))
            {
                _outputWriter.WriteLine($"    Target: {hint.Target}");
            }
        }
    }

    private static string RenderValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string RenderList(IReadOnlyList<string>? values)
    {
        return values is null || values.Count == 0
            ? "-"
            : string.Join(", ", values);
    }
}
