using System.Reflection;
using System.Text.Json;

namespace Fabricator.Core.Templates;

public sealed class LocalTemplatePackageProvider : ITemplatePackageProvider
{
    public const string DefaultTemplatesDirectoryName = "Templates";
    public const string ManifestFileName = "template.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _templatesRootDirectory;

    public LocalTemplatePackageProvider()
        : this(Path.Combine(AppContext.BaseDirectory, DefaultTemplatesDirectoryName))
    {
    }

    public LocalTemplatePackageProvider(string templatesRootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatesRootDirectory);
        _templatesRootDirectory = Path.GetFullPath(templatesRootDirectory);
    }

    public TemplatePackage GetTemplate(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        if (templateId.Contains('/') || templateId.Contains('\\'))
        {
            throw new TemplatePackageException("Template id must not contain path separators.");
        }

        var templateRootDirectory = Path.GetFullPath(Path.Combine(_templatesRootDirectory, templateId));

        if (!IsChildPath(_templatesRootDirectory, templateRootDirectory))
        {
            throw new TemplatePackageException("Template directory resolved outside the templates root.");
        }

        var manifestPath = Path.Combine(templateRootDirectory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            throw new TemplatePackageException($"Template manifest was not found: {manifestPath}");
        }

        var manifest = JsonSerializer.Deserialize<TemplateManifest>(
            File.ReadAllText(manifestPath),
            SerializerOptions);

        if (manifest is null)
        {
            throw new TemplatePackageException($"Template manifest could not be read: {manifestPath}");
        }

        if (!string.Equals(templateId, manifest.Id, StringComparison.Ordinal))
        {
            throw new TemplatePackageException($"Template manifest id '{manifest.Id}' does not match requested template '{templateId}'.");
        }

        return new TemplatePackage(manifest, templateRootDirectory);
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
