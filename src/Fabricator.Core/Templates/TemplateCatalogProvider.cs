using System.Text.Json;

namespace Fabricator.Core.Templates;

public sealed class TemplateCatalogProvider : ITemplateCatalogProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public TemplateCatalogProvider()
        : this(new HttpClient())
    {
    }

    public TemplateCatalogProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FabricatorTemplateCatalog> ListTemplatesAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return IsHttpSource(source)
            ? await GetRemoteCatalogAsync(source, cancellationToken)
            : await GetLocalCatalogAsync(source, cancellationToken);
    }

    public async Task<FabricatorTemplatePackage> GetTemplateAsync(
        string source,
        string templateId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        return IsHttpSource(source)
            ? await GetRemoteTemplateAsync(source, templateId, cancellationToken)
            : await GetLocalTemplateAsync(source, templateId, cancellationToken);
    }

    private static async Task<FabricatorTemplatePackage> GetLocalTemplateAsync(
        string source,
        string templateId,
        CancellationToken cancellationToken)
    {
        var catalogPath = Path.GetFullPath(source);

        var catalog = await GetLocalCatalogAsync(source, cancellationToken);
        var entry = FindCatalogEntry(catalog, templateId);
        var catalogDirectory = Path.GetDirectoryName(catalogPath) ?? Directory.GetCurrentDirectory();
        var manifestPath = Path.GetFullPath(Path.Combine(catalogDirectory, entry.Manifest));

        if (!IsChildPath(catalogDirectory, manifestPath))
        {
            throw new TemplatePackageException($"Template manifest resolved outside the catalog directory: {entry.Manifest}");
        }

        if (!File.Exists(manifestPath))
        {
            throw new TemplatePackageException($"Template manifest was not found: {manifestPath}");
        }

        var manifest = DeserializeManifest(await File.ReadAllTextAsync(manifestPath, cancellationToken), manifestPath);
        ValidateManifest(manifest, templateId);

        var manifestDirectory = Path.GetDirectoryName(manifestPath) ?? catalogDirectory;
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in manifest.Files)
        {
            var sourcePath = Path.GetFullPath(Path.Combine(manifestDirectory, file.Path));

            if (!IsChildPath(manifestDirectory, sourcePath))
            {
                throw new TemplatePackageException($"Template file resolved outside the template directory: {file.Path}");
            }

            if (!File.Exists(sourcePath))
            {
                throw new TemplatePackageException($"Template file was not found: {sourcePath}");
            }

            files[file.Path] = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        }

        return new FabricatorTemplatePackage(manifest, files);
    }

    private async Task<FabricatorTemplatePackage> GetRemoteTemplateAsync(
        string source,
        string templateId,
        CancellationToken cancellationToken)
    {
        var catalogUri = new Uri(source, UriKind.Absolute);
        var catalog = await GetRemoteCatalogAsync(source, cancellationToken);
        var entry = FindCatalogEntry(catalog, templateId);
        var manifestUri = new Uri(catalogUri, entry.Manifest);
        var manifest = DeserializeManifest(await _httpClient.GetStringAsync(manifestUri, cancellationToken), manifestUri.ToString());
        ValidateManifest(manifest, templateId);

        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in manifest.Files)
        {
            var fileUri = new Uri(manifestUri, file.Path);
            files[file.Path] = await _httpClient.GetStringAsync(fileUri, cancellationToken);
        }

        return new FabricatorTemplatePackage(manifest, files);
    }

    private static async Task<FabricatorTemplateCatalog> GetLocalCatalogAsync(
        string source,
        CancellationToken cancellationToken)
    {
        var catalogPath = Path.GetFullPath(source);

        if (!File.Exists(catalogPath))
        {
            throw new TemplatePackageException($"Template catalog was not found: {catalogPath}");
        }

        return DeserializeCatalog(await File.ReadAllTextAsync(catalogPath, cancellationToken), catalogPath);
    }

    private async Task<FabricatorTemplateCatalog> GetRemoteCatalogAsync(
        string source,
        CancellationToken cancellationToken)
    {
        return DeserializeCatalog(await _httpClient.GetStringAsync(new Uri(source, UriKind.Absolute), cancellationToken), source);
    }

    private static FabricatorTemplateCatalog DeserializeCatalog(string json, string source)
    {
        return JsonSerializer.Deserialize<FabricatorTemplateCatalog>(json, SerializerOptions)
            ?? throw new TemplatePackageException($"Template catalog could not be read: {source}");
    }

    private static FabricatorTemplateManifest DeserializeManifest(string json, string source)
    {
        return JsonSerializer.Deserialize<FabricatorTemplateManifest>(json, SerializerOptions)
            ?? throw new TemplatePackageException($"Template manifest could not be read: {source}");
    }

    private static FabricatorTemplateCatalogEntry FindCatalogEntry(
        FabricatorTemplateCatalog catalog,
        string templateId)
    {
        return catalog.Templates.SingleOrDefault(template => string.Equals(template.Id, templateId, StringComparison.Ordinal))
            ?? throw new TemplatePackageException($"Template '{templateId}' was not found in the catalog.");
    }

    private static void ValidateManifest(FabricatorTemplateManifest manifest, string templateId)
    {
        if (!string.Equals(manifest.Id, templateId, StringComparison.Ordinal))
        {
            throw new TemplatePackageException($"Template manifest id '{manifest.Id}' does not match requested template '{templateId}'.");
        }
    }

    private static bool IsHttpSource(string source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
