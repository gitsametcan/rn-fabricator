using Fabricator.Core.Projects;

namespace Fabricator.Core.Templates;

public sealed class FabricatorTemplateApplyService
{
    private readonly IFabricatorProjectCompatibilityValidator _compatibilityValidator;

    public FabricatorTemplateApplyService()
        : this(new FabricatorProjectCompatibilityValidator())
    {
    }

    public FabricatorTemplateApplyService(IFabricatorProjectCompatibilityValidator compatibilityValidator)
    {
        _compatibilityValidator = compatibilityValidator;
    }

    public async Task<FabricatorTemplateApplyResult> ApplyAsync(
        FabricatorTemplateApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var targetRoot = Path.GetFullPath(request.TargetDirectory);
        var compatibility = _compatibilityValidator.Validate(targetRoot);

        if (!compatibility.IsCompatible)
        {
            return new FabricatorTemplateApplyResult(
                [],
                [],
                compatibility.Errors);
        }

        var generatedFiles = new List<string>();
        var skippedFiles = new List<string>();
        var errors = new List<string>();
        var manifest = compatibility.Manifest
            ?? throw new InvalidOperationException("Compatible Fabricator projects must include a manifest.");

        foreach (var file in request.Package.Manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ValidateTargetFolder(file, manifest, errors))
            {
                continue;
            }

            if (!request.Package.Files.TryGetValue(file.Path, out var contents))
            {
                errors.Add($"Template source file was not found in package: {file.Path}");
                continue;
            }

            var targetRelativePath = string.IsNullOrWhiteSpace(file.TargetPath)
                ? file.Path
                : file.TargetPath;

            if (!TryResolveTargetPath(targetRoot, targetRelativePath, out var targetPath, out var targetError))
            {
                errors.Add($"Invalid target path for '{file.Path}': {targetError}");
                continue;
            }

            if (File.Exists(targetPath) && !request.OverwriteExistingFiles)
            {
                skippedFiles.Add(targetRelativePath);
                continue;
            }

            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            await File.WriteAllTextAsync(targetPath, contents, cancellationToken);
            generatedFiles.Add(targetRelativePath);
        }

        return new FabricatorTemplateApplyResult(generatedFiles, skippedFiles, errors);
    }

    private static bool ValidateTargetFolder(
        FabricatorTemplateFile file,
        FabricatorProjectManifest manifest,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(file.TargetFolder))
        {
            return true;
        }

        if (manifest.Folders.Any(folder => string.Equals(folder.Key, file.TargetFolder, StringComparison.Ordinal)))
        {
            return true;
        }

        errors.Add($"Template file '{file.Path}' targets unknown Fabricator folder: {file.TargetFolder}");
        return false;
    }

    private static bool TryResolveTargetPath(
        string targetRoot,
        string relativePath,
        out string targetPath,
        out string error)
    {
        targetPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            error = "Target path is required.";
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            error = "Target path must be relative to the project root.";
            return false;
        }

        targetPath = Path.GetFullPath(Path.Combine(targetRoot, relativePath));

        if (!IsChildPath(targetRoot, targetPath))
        {
            error = "Target path resolved outside the project root.";
            return false;
        }

        return true;
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
