using Fabricator.Core.Projects;
using System.Text.Json;

namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateCaptureService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IFabricatorProjectCompatibilityValidator _compatibilityValidator;

    public FabricatorTemplateCaptureService()
        : this(new FabricatorProjectCompatibilityValidator())
    {
    }

    public FabricatorTemplateCaptureService(IFabricatorProjectCompatibilityValidator compatibilityValidator)
    {
        _compatibilityValidator = compatibilityValidator;
    }

    public async Task<FabricatorTemplateCaptureResult> CaptureAsync(
        FabricatorTemplateCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = ValidateRequest(request);
        if (errors.Count > 0)
        {
            return Failed(request, errors);
        }

        var projectRoot = Path.GetFullPath(request.SourceProjectDirectory);
        var outputRoot = Path.GetFullPath(request.OutputDirectory);
        var templateDirectory = Path.GetFullPath(Path.Combine(outputRoot, request.TemplateId));
        var manifestPath = Path.Combine(templateDirectory, "fabricator-template.json");

        if (!IsChildPath(outputRoot, templateDirectory))
        {
            errors.Add("Template output directory resolved outside the requested output root.");
            return Failed(request, errors);
        }

        if (Directory.Exists(templateDirectory) || File.Exists(templateDirectory))
        {
            errors.Add($"Template output already exists: {templateDirectory}");
            return Failed(request, errors);
        }

        var compatibility = _compatibilityValidator.Validate(projectRoot);
        if (!compatibility.IsCompatible)
        {
            return Failed(request, compatibility.Errors);
        }

        var manifest = compatibility.Manifest
            ?? throw new InvalidOperationException("Compatible Fabricator projects must include a manifest.");
        var folder = manifest.Folders.FirstOrDefault(folder =>
            string.Equals(folder.Key, request.Category, StringComparison.Ordinal));

        if (folder is null)
        {
            errors.Add($"Category must match a Fabricator project folder key: {request.Category}");
            return Failed(request, errors);
        }

        if (!TryResolveProjectPath(projectRoot, folder.Path, out var captureRoot, out var captureRootError))
        {
            errors.Add($"Invalid capture folder '{folder.Path}': {captureRootError}");
            return Failed(request, errors);
        }

        var capturedFiles = Directory
            .EnumerateFiles(captureRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetFullPath(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (capturedFiles.Length == 0)
        {
            errors.Add($"No files were found in Fabricator folder: {folder.Path}");
            return Failed(request, errors);
        }

        var templateFiles = new List<FabricatorTemplateFile>();
        foreach (var capturedFile in capturedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsChildPath(projectRoot, capturedFile))
            {
                errors.Add($"Captured file resolved outside the project root: {capturedFile}");
                continue;
            }

            var relativePath = Path.GetRelativePath(projectRoot, capturedFile).Replace('\\', '/');
            templateFiles.Add(new FabricatorTemplateFile(
                relativePath,
                "file",
                relativePath,
                folder.Key,
                $"Captured from {relativePath}."));
        }

        if (errors.Count > 0)
        {
            return Failed(request, errors);
        }

        var templateManifest = new FabricatorTemplateManifest(
            2,
            "fabricator-template",
            request.TemplateId,
            ToDisplayName(request.TemplateId),
            $"Captured {request.Category} template from a Fabricator project.",
            "0.1.0",
            "apply",
            templateFiles,
            Category: request.Category,
            Tags: [request.Category]);

        if (request.DryRun)
        {
            return new FabricatorTemplateCaptureResult(
                request.TemplateId,
                templateDirectory,
                manifestPath,
                templateFiles.Select(file => file.Path).ToArray(),
                [],
                dryRun: true);
        }

        var stagingDirectory = Path.Combine(outputRoot, $".{SanitizePathSegment(request.TemplateId)}.tmp-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(stagingDirectory);

            foreach (var file in templateFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourcePath = Path.Combine(projectRoot, file.Path);
                var targetPath = Path.GetFullPath(Path.Combine(stagingDirectory, file.Path));

                if (!IsChildPath(stagingDirectory, targetPath))
                {
                    errors.Add($"Template file resolved outside the staging directory: {file.Path}");
                    continue;
                }

                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath);
            }

            if (errors.Count > 0)
            {
                return Failed(request, errors);
            }

            await File.WriteAllTextAsync(
                Path.Combine(stagingDirectory, "fabricator-template.json"),
                JsonSerializer.Serialize(templateManifest, SerializerOptions),
                cancellationToken);

            Directory.CreateDirectory(outputRoot);
            Directory.Move(stagingDirectory, templateDirectory);

            return new FabricatorTemplateCaptureResult(
                request.TemplateId,
                templateDirectory,
                manifestPath,
                templateFiles.Select(file => file.Path).ToArray(),
                []);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed(request, [$"Template capture failed: {exception.Message}"]);
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    private static List<string> ValidateRequest(FabricatorTemplateCaptureRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            errors.Add("Template id is required.");
        }
        else if (Path.IsPathRooted(request.TemplateId) ||
                 request.TemplateId.Split(['/', '\\']).Any(segment => segment is "" or "." or ".."))
        {
            errors.Add("Template id must be a relative path and must not contain empty, current, or parent segments.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            errors.Add("Template category is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceProjectDirectory))
        {
            errors.Add("Source project directory is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            errors.Add("Output directory is required.");
        }

        return errors;
    }

    private static FabricatorTemplateCaptureResult Failed(
        FabricatorTemplateCaptureRequest request,
        IReadOnlyList<string> errors)
    {
        var outputRoot = string.IsNullOrWhiteSpace(request.OutputDirectory)
            ? string.Empty
            : Path.GetFullPath(request.OutputDirectory);
        var templateDirectory = string.IsNullOrWhiteSpace(outputRoot) || string.IsNullOrWhiteSpace(request.TemplateId)
            ? string.Empty
            : Path.GetFullPath(Path.Combine(outputRoot, request.TemplateId));
        var manifestPath = string.IsNullOrWhiteSpace(templateDirectory)
            ? string.Empty
            : Path.Combine(templateDirectory, "fabricator-template.json");

        return new FabricatorTemplateCaptureResult(
            request.TemplateId,
            templateDirectory,
            manifestPath,
            [],
            errors,
            request.DryRun);
    }

    private static bool TryResolveProjectPath(
        string projectRoot,
        string relativePath,
        out string resolvedPath,
        out string error)
    {
        resolvedPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            error = "Path is required.";
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            error = "Path must be relative to the project root.";
            return false;
        }

        resolvedPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        if (!IsChildPath(projectRoot, resolvedPath))
        {
            error = "Path resolved outside the project root.";
            return false;
        }

        return true;
    }

    private static string ToDisplayName(string templateId)
    {
        var lastSegment = templateId.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? templateId;
        return string.Join(
            ' ',
            lastSegment
                .Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));
    }

    private static string SanitizePathSegment(string value)
    {
        return string.Join("-", value.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries));
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
}
