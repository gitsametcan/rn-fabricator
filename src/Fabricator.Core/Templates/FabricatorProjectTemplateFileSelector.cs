using Fabricator.Core.Projects;

namespace Fabricator.Core.Templates;

internal static class FabricatorProjectTemplateFileSelector
{
    public static ProjectTemplateFileSelection Select(
        FabricatorProjectManifest manifest,
        string projectRoot,
        string category,
        IReadOnlyList<string>? includePaths,
        CancellationToken cancellationToken = default)
    {
        var categoryFolder = manifest.Folders.FirstOrDefault(folder =>
            string.Equals(folder.Key, category, StringComparison.Ordinal));

        if (categoryFolder is null)
        {
            return ProjectTemplateFileSelection.Failed([$"Category must match a Fabricator project folder key: {category}"]);
        }

        return includePaths is null || includePaths.Count == 0
            ? SelectCategoryFolder(projectRoot, categoryFolder, cancellationToken)
            : SelectIncludedFiles(projectRoot, manifest.Folders, includePaths, cancellationToken);
    }

    private static ProjectTemplateFileSelection SelectCategoryFolder(
        string projectRoot,
        FabricatorProjectFolder folder,
        CancellationToken cancellationToken)
    {
        if (!TryResolveProjectPath(projectRoot, folder.Path, out var captureRoot, out var captureRootError))
        {
            return ProjectTemplateFileSelection.Failed([$"Invalid capture folder '{folder.Path}': {captureRootError}"]);
        }

        var capturedFiles = Directory
            .EnumerateFiles(captureRoot, "*", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (capturedFiles.Length == 0)
        {
            return ProjectTemplateFileSelection.Failed([$"No files were found in Fabricator folder: {folder.Path}"]);
        }

        var templateFiles = new List<FabricatorTemplateFile>();

        foreach (var capturedFile in capturedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsChildPath(projectRoot, capturedFile))
            {
                return ProjectTemplateFileSelection.Failed([$"Captured file resolved outside the project root: {capturedFile}"]);
            }

            var relativePath = NormalizeRelativePath(Path.GetRelativePath(projectRoot, capturedFile));
            templateFiles.Add(CreateTemplateFile(relativePath, folder.Key));
        }

        return ProjectTemplateFileSelection.Success(templateFiles);
    }

    private static ProjectTemplateFileSelection SelectIncludedFiles(
        string projectRoot,
        IReadOnlyList<FabricatorProjectFolder> folders,
        IReadOnlyList<string> includePaths,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var selected = new List<FabricatorTemplateFile>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var orderedFolders = folders
            .OrderByDescending(folder => NormalizeRelativePath(folder.Path).Length)
            .ToArray();

        foreach (var includePath in includePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryResolveIncludedFile(projectRoot, includePath, out var resolvedPath, out var relativePath, out var error))
            {
                errors.Add(error);
                continue;
            }

            if (!seen.Add(relativePath))
            {
                continue;
            }

            var targetFolder = ResolveTargetFolder(relativePath, orderedFolders);
            if (targetFolder is null)
            {
                errors.Add($"Included file must be under a Fabricator project folder: {relativePath}");
                continue;
            }

            if (Directory.Exists(resolvedPath))
            {
                errors.Add($"Included path must be a file, not a directory: {relativePath}");
                continue;
            }

            if (!File.Exists(resolvedPath))
            {
                errors.Add($"Included file was not found: {relativePath}");
                continue;
            }

            selected.Add(CreateTemplateFile(relativePath, targetFolder.Key));
        }

        return errors.Count > 0
            ? ProjectTemplateFileSelection.Failed(errors)
            : ProjectTemplateFileSelection.Success(selected);
    }

    private static bool TryResolveIncludedFile(
        string projectRoot,
        string includePath,
        out string resolvedPath,
        out string relativePath,
        out string error)
    {
        resolvedPath = string.Empty;
        relativePath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(includePath))
        {
            error = "Included file path is required.";
            return false;
        }

        if (Path.IsPathRooted(includePath))
        {
            error = $"Included file path must be relative to the project root: {includePath}";
            return false;
        }

        var segments = includePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            error = $"Included file path must not contain current or parent segments: {includePath}";
            return false;
        }

        resolvedPath = Path.GetFullPath(Path.Combine(projectRoot, includePath));
        if (!IsChildPath(projectRoot, resolvedPath))
        {
            error = $"Included file resolved outside the project root: {includePath}";
            return false;
        }

        relativePath = NormalizeRelativePath(Path.GetRelativePath(projectRoot, resolvedPath));
        return true;
    }

    private static FabricatorProjectFolder? ResolveTargetFolder(
        string relativePath,
        IReadOnlyList<FabricatorProjectFolder> folders)
    {
        return folders.FirstOrDefault(folder =>
        {
            var folderPath = NormalizeRelativePath(folder.Path);
            return string.Equals(relativePath, folderPath, StringComparison.Ordinal) ||
                   relativePath.StartsWith($"{folderPath}/", StringComparison.Ordinal);
        });
    }

    private static FabricatorTemplateFile CreateTemplateFile(string relativePath, string targetFolder)
    {
        return new FabricatorTemplateFile(
            relativePath,
            "file",
            relativePath,
            targetFolder,
            $"Captured from {relativePath}.");
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

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

internal sealed record ProjectTemplateFileSelection(
    IReadOnlyList<FabricatorTemplateFile> Files,
    IReadOnlyList<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public static ProjectTemplateFileSelection Success(IReadOnlyList<FabricatorTemplateFile> files)
    {
        return new ProjectTemplateFileSelection(files, []);
    }

    public static ProjectTemplateFileSelection Failed(IReadOnlyList<string> errors)
    {
        return new ProjectTemplateFileSelection([], errors);
    }
}
