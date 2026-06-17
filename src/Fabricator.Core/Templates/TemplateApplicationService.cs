namespace Fabricator.Core.Templates;

public sealed class TemplateApplicationService
{
    public TemplateApplicationResult Apply(TemplateApplicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var generatedFiles = new List<string>();
        var skippedFiles = new List<string>();
        var errors = new List<string>();
        var packageRoot = Path.GetFullPath(request.Package.RootDirectory);
        var targetRoot = Path.GetFullPath(request.TargetDirectory);

        Directory.CreateDirectory(targetRoot);

        foreach (var relativePath in request.Package.Manifest.Files)
        {
            if (!TryResolveFilePaths(packageRoot, targetRoot, relativePath, out var sourcePath, out var targetPath, out var error))
            {
                errors.Add(error);
                continue;
            }

            if (!File.Exists(sourcePath))
            {
                errors.Add($"Template source file was not found: {relativePath}");
                continue;
            }

            if (File.Exists(targetPath) && !request.OverwriteExistingFiles)
            {
                skippedFiles.Add(relativePath);
                errors.Add($"Target file already exists and was not overwritten: {relativePath}");
                continue;
            }

            var targetDirectory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(sourcePath, targetPath, overwrite: request.OverwriteExistingFiles);
            generatedFiles.Add(relativePath);
        }

        return new TemplateApplicationResult(generatedFiles, skippedFiles, errors);
    }

    private static bool TryResolveFilePaths(
        string packageRoot,
        string targetRoot,
        string relativePath,
        out string sourcePath,
        out string targetPath,
        out string error)
    {
        sourcePath = Path.GetFullPath(Path.Combine(packageRoot, relativePath));
        targetPath = Path.GetFullPath(Path.Combine(targetRoot, relativePath));
        error = string.Empty;

        if (!IsChildPath(packageRoot, sourcePath))
        {
            error = $"Template source file resolved outside the package root: {relativePath}";
            return false;
        }

        if (!IsChildPath(targetRoot, targetPath))
        {
            error = $"Template target file resolved outside the target directory: {relativePath}";
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
