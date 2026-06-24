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
                [],
                [],
                [],
                compatibility.Errors);
        }

        var generatedFiles = new List<string>();
        var skippedFiles = new List<string>();
        var appliedExports = new List<string>();
        var skippedExports = new List<string>();
        var integrationReports = new List<FabricatorTemplateIntegrationReport>();
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

        if (errors.Count == 0)
        {
            await ApplyExportsAsync(
                request.Package.Manifest.Exports ?? [],
                manifest,
                targetRoot,
                appliedExports,
                skippedExports,
                integrationReports,
                cancellationToken);
        }

        ReportIntegrationHints(request.Package.Manifest.IntegrationHints ?? [], integrationReports);

        return new FabricatorTemplateApplyResult(
            generatedFiles,
            skippedFiles,
            appliedExports,
            skippedExports,
            integrationReports,
            errors);
    }

    private static async Task ApplyExportsAsync(
        IReadOnlyList<FabricatorTemplateExport> exports,
        FabricatorProjectManifest manifest,
        string targetRoot,
        List<string> appliedExports,
        List<string> skippedExports,
        List<FabricatorTemplateIntegrationReport> integrationReports,
        CancellationToken cancellationToken)
    {
        foreach (var templateExport in exports)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var statement = templateExport.Statement?.Trim();
            if (!IsSafeBarrelExportStatement(statement))
            {
                integrationReports.Add(new FabricatorTemplateIntegrationReport(
                    "unsupported-export",
                    $"Export statement is not a supported single-line barrel export: {templateExport.Statement}",
                    templateExport.IntegrationPoint));
                continue;
            }

            var integrationPoint = manifest.IntegrationPoints
                .FirstOrDefault(point => string.Equals(point.Key, templateExport.IntegrationPoint, StringComparison.Ordinal));

            if (integrationPoint is null)
            {
                integrationReports.Add(new FabricatorTemplateIntegrationReport(
                    "unsupported-export",
                    $"Template export targets unknown integration point: {templateExport.IntegrationPoint}",
                    templateExport.IntegrationPoint));
                continue;
            }

            if (!string.Equals(integrationPoint.Type, FabricatorProjectContract.BarrelExportIntegrationType, StringComparison.Ordinal))
            {
                integrationReports.Add(new FabricatorTemplateIntegrationReport(
                    "unsupported-export",
                    $"Integration point '{integrationPoint.Key}' has unsupported type '{integrationPoint.Type}'.",
                    integrationPoint.Key));
                continue;
            }

            if (!TryResolveTargetPath(targetRoot, integrationPoint.Path, out var integrationPath, out var integrationError))
            {
                integrationReports.Add(new FabricatorTemplateIntegrationReport(
                    "unsupported-export",
                    $"Integration point '{integrationPoint.Key}' has invalid path: {integrationError}",
                    integrationPoint.Key));
                continue;
            }

            var existingContent = await File.ReadAllTextAsync(integrationPath, cancellationToken);
            var existingLines = existingContent
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Select(line => line.Trim())
                .ToArray();

            if (existingLines.Any(line => string.Equals(line, statement, StringComparison.Ordinal)))
            {
                skippedExports.Add($"{integrationPoint.Key}: {statement}");
                continue;
            }

            var separator = existingContent.Length == 0 || existingContent.EndsWith('\n')
                ? string.Empty
                : "\n";
            await File.AppendAllTextAsync(integrationPath, $"{separator}{statement}\n", cancellationToken);
            appliedExports.Add($"{integrationPoint.Key}: {statement}");
        }
    }

    private static void ReportIntegrationHints(
        IReadOnlyList<FabricatorTemplateIntegrationHint> integrationHints,
        List<FabricatorTemplateIntegrationReport> integrationReports)
    {
        foreach (var hint in integrationHints)
        {
            var kind = string.Equals(hint.Type, "manual", StringComparison.OrdinalIgnoreCase)
                ? "manual"
                : "unsupported-integration";
            var message = string.Equals(hint.Type, "manual", StringComparison.OrdinalIgnoreCase)
                ? hint.Message
                : $"Integration hint type '{hint.Type}' is not automated yet: {hint.Message}";

            integrationReports.Add(new FabricatorTemplateIntegrationReport(kind, message, hint.Target));
        }
    }

    private static bool IsSafeBarrelExportStatement(string? statement)
    {
        if (string.IsNullOrWhiteSpace(statement))
        {
            return false;
        }

        return statement.StartsWith("export ", StringComparison.Ordinal)
            && statement.Contains(" from ", StringComparison.Ordinal)
            && statement.EndsWith(';')
            && statement.Count(character => character == ';') == 1
            && !statement.Contains('`')
            && !statement.Contains('\n')
            && !statement.Contains('\r');
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
