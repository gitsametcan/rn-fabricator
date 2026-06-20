using Fabricator.Core;
using Fabricator.Core.Templates;

namespace Fabricator.Cli.Templates;

public sealed class TemplatesCopyCommandHandler
{
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;
    private readonly ITemplateCatalogProvider _templateCatalogProvider;

    public TemplatesCopyCommandHandler(
        ITemplateCatalogProvider templateCatalogProvider,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _templateCatalogProvider = templateCatalogProvider;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string templateId,
        string source,
        string outputDirectory,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            _errorWriter.WriteLine("Template id is required.");
            return ExitCodes.InvalidInput;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            _errorWriter.WriteLine("Template source is required. Pass --source <catalog-url-or-path>.");
            return ExitCodes.InvalidInput;
        }

        try
        {
            var package = await _templateCatalogProvider.GetTemplateAsync(source, templateId, cancellationToken);
            var result = await CopyTemplateAsync(package, outputDirectory, overwrite, cancellationToken);
            RenderResult(source, outputDirectory, overwrite, package, result);

            return result.Errors.Count == 0 ? ExitCodes.Success : ExitCodes.GeneralFailure;
        }
        catch (TemplatePackageException exception)
        {
            _errorWriter.WriteLine("Template could not be read.");
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

    private static async Task<TemplateCopyResult> CopyTemplateAsync(
        FabricatorTemplatePackage package,
        string outputDirectory,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var generatedFiles = new List<string>();
        var skippedFiles = new List<string>();
        var errors = new List<string>();
        var targetRoot = Path.GetFullPath(outputDirectory);

        Directory.CreateDirectory(targetRoot);

        foreach (var file in package.Files)
        {
            var targetPath = Path.GetFullPath(Path.Combine(targetRoot, file.Key));

            if (!IsChildPath(targetRoot, targetPath))
            {
                errors.Add($"Template file resolved outside the output directory: {file.Key}");
                continue;
            }

            if (File.Exists(targetPath) && !overwrite)
            {
                skippedFiles.Add(file.Key);
                continue;
            }

            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            await File.WriteAllTextAsync(targetPath, file.Value, cancellationToken);
            generatedFiles.Add(file.Key);
        }

        return new TemplateCopyResult(generatedFiles, skippedFiles, errors);
    }

    private void RenderResult(
        string source,
        string outputDirectory,
        bool overwrite,
        FabricatorTemplatePackage package,
        TemplateCopyResult result)
    {
        _outputWriter.WriteLine($"Template copied: {package.Manifest.Id}");
        _outputWriter.WriteLine($"Source: {source}");
        _outputWriter.WriteLine($"Output directory: {Path.GetFullPath(outputDirectory)}");
        _outputWriter.WriteLine($"Overwrite: {(overwrite ? "yes" : "no")}");
        _outputWriter.WriteLine();
        _outputWriter.WriteLine($"Generated: {result.GeneratedFiles.Count}");

        foreach (var file in result.GeneratedFiles)
        {
            _outputWriter.WriteLine($"  + {file}");
        }

        _outputWriter.WriteLine($"Skipped: {result.SkippedFiles.Count}");

        foreach (var file in result.SkippedFiles)
        {
            _outputWriter.WriteLine($"  - {file}");
        }

        if (result.Errors.Count == 0)
        {
            return;
        }

        _errorWriter.WriteLine("Template copy completed with errors.");
        foreach (var error in result.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private static bool IsChildPath(string parentPath, string childPath)
    {
        var parent = EnsureTrailingSeparator(Path.GetFullPath(parentPath));
        var child = Path.GetFullPath(childPath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return child.StartsWith(parent, comparison);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private sealed record TemplateCopyResult(
        IReadOnlyList<string> GeneratedFiles,
        IReadOnlyList<string> SkippedFiles,
        IReadOnlyList<string> Errors);
}
