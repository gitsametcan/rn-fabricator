using System.Text.Json;

namespace Fabricator.Core.Projects;

public sealed class FabricatorProjectCompatibilityValidator : IFabricatorProjectCompatibilityValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public FabricatorProjectCompatibilityResult Validate(string projectDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        var projectRoot = Path.GetFullPath(projectDirectory);
        var errors = new List<string>();

        if (!Directory.Exists(projectRoot))
        {
            return FabricatorProjectCompatibilityResult.Incompatible(
                projectRoot,
                null,
                [$"Project directory was not found: {projectRoot}"]);
        }

        var manifestPath = Path.Combine(projectRoot, FabricatorProjectContract.ManifestRelativePath);
        if (!File.Exists(manifestPath))
        {
            return FabricatorProjectCompatibilityResult.Incompatible(
                projectRoot,
                null,
                [$"Fabricator project manifest was not found: {FabricatorProjectContract.ManifestRelativePath}"]);
        }

        var manifest = ReadManifest(manifestPath, errors);
        if (manifest is null)
        {
            return FabricatorProjectCompatibilityResult.Incompatible(projectRoot, null, errors);
        }

        ValidateManifestIdentity(manifest, errors);
        ValidateSourceRoot(projectRoot, manifest, errors);
        ValidateFolders(projectRoot, manifest, errors);
        ValidateIntegrationPoints(projectRoot, manifest, errors);

        return errors.Count == 0
            ? FabricatorProjectCompatibilityResult.Compatible(projectRoot, manifest)
            : FabricatorProjectCompatibilityResult.Incompatible(projectRoot, manifest, errors);
    }

    private static FabricatorProjectManifest? ReadManifest(string manifestPath, List<string> errors)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<FabricatorProjectManifest>(
                File.ReadAllText(manifestPath),
                SerializerOptions);

            if (manifest is null)
            {
                errors.Add("Fabricator project manifest could not be parsed.");
            }

            return manifest;
        }
        catch (JsonException exception)
        {
            errors.Add($"Fabricator project manifest could not be parsed: {exception.Message}");
            return null;
        }
        catch (IOException exception)
        {
            errors.Add($"Fabricator project manifest could not be read: {exception.Message}");
            return null;
        }
        catch (UnauthorizedAccessException exception)
        {
            errors.Add($"Fabricator project manifest could not be read: {exception.Message}");
            return null;
        }
    }

    private static void ValidateManifestIdentity(
        FabricatorProjectManifest manifest,
        List<string> errors)
    {
        if (manifest.SchemaVersion != FabricatorProjectContract.CurrentSchemaVersion)
        {
            errors.Add($"Unsupported Fabricator project schema version: {manifest.SchemaVersion}.");
        }

        if (!string.Equals(manifest.Kind, FabricatorProjectContract.ProjectKind, StringComparison.Ordinal))
        {
            errors.Add($"Unsupported Fabricator project kind: {manifest.Kind}.");
        }

        if (!string.Equals(manifest.ProjectType, FabricatorProjectContract.ProjectType, StringComparison.Ordinal))
        {
            errors.Add($"Unsupported Fabricator project type: {manifest.ProjectType}.");
        }
    }

    private static void ValidateSourceRoot(
        string projectRoot,
        FabricatorProjectManifest manifest,
        List<string> errors)
    {
        if (!TryResolveRelativeProjectPath(projectRoot, manifest.SourceRoot, out var sourceRoot, out var sourceRootError))
        {
            errors.Add($"Invalid source root '{manifest.SourceRoot}': {sourceRootError}");
            return;
        }

        if (!Directory.Exists(sourceRoot))
        {
            errors.Add($"Source root was not found: {manifest.SourceRoot}");
        }
    }

    private static void ValidateFolders(
        string projectRoot,
        FabricatorProjectManifest manifest,
        List<string> errors)
    {
        if (manifest.Folders is null || manifest.Folders.Count == 0)
        {
            errors.Add("Fabricator project manifest does not declare any folders.");
            return;
        }

        foreach (var folder in manifest.Folders)
        {
            if (!TryResolveRelativeProjectPath(projectRoot, folder.Path, out var folderPath, out var folderError))
            {
                errors.Add($"Invalid folder path for '{folder.Key}': {folderError}");
                continue;
            }

            if (!Directory.Exists(folderPath))
            {
                errors.Add($"Required Fabricator folder was not found: {folder.Path}");
            }
        }
    }

    private static void ValidateIntegrationPoints(
        string projectRoot,
        FabricatorProjectManifest manifest,
        List<string> errors)
    {
        if (manifest.IntegrationPoints is null)
        {
            errors.Add("Fabricator project manifest does not declare any integration points.");
            return;
        }

        foreach (var integrationPoint in manifest.IntegrationPoints)
        {
            if (!TryResolveRelativeProjectPath(projectRoot, integrationPoint.Path, out var integrationPath, out var integrationError))
            {
                errors.Add($"Invalid integration point path for '{integrationPoint.Key}': {integrationError}");
                continue;
            }

            if (!string.Equals(integrationPoint.Type, FabricatorProjectContract.BarrelExportIntegrationType, StringComparison.Ordinal))
            {
                errors.Add($"Unsupported Fabricator integration point type '{integrationPoint.Type}' for '{integrationPoint.Key}'.");
                continue;
            }

            if (!File.Exists(integrationPath))
            {
                errors.Add($"Required Fabricator integration point was not found: {integrationPoint.Path}");
            }
        }
    }

    private static bool TryResolveRelativeProjectPath(
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
}
