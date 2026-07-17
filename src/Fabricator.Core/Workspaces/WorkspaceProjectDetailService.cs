using Fabricator.Core.Projects;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Fabricator.Core.Workspaces;

public sealed class WorkspaceProjectDetailService : IWorkspaceProjectDetailService
{
    private readonly IApplicationCommandCenterMetadataService _commandCenterMetadataService;

    public WorkspaceProjectDetailService()
        : this(new ApplicationCommandCenterMetadataService())
    {
    }

    public WorkspaceProjectDetailService(IApplicationCommandCenterMetadataService commandCenterMetadataService)
    {
        _commandCenterMetadataService = commandCenterMetadataService;
    }

    public WorkspaceProjectDetail GetDetail(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var fullProjectPath = Path.GetFullPath(projectPath);
        var projectExists = Directory.Exists(fullProjectPath);
        var hasFabricatorManifest = File.Exists(Path.Combine(
            fullProjectPath,
            FabricatorProjectContract.ManifestRelativePath));
        var hasFabricatorState = File.Exists(Path.Combine(
            fullProjectPath,
            FabricatorProjectStateContract.StateRelativePath));
        var state = ReadFabricatorState(fullProjectPath);
        var package = ReadPackageJson(fullProjectPath);
        var storeMetadata = ReadStoreMetadata(fullProjectPath, package.Version);

        return new WorkspaceProjectDetail(
            fullProjectPath,
            projectExists,
            state.ProjectName ?? ReadAppJsonName(fullProjectPath) ?? Path.GetFileName(fullProjectPath),
            package.PackageName,
            hasFabricatorManifest,
            hasFabricatorState,
            state.AppliedTemplateCount,
            ReadStatistics(fullProjectPath),
            storeMetadata,
            ReadAgentMemory(fullProjectPath),
            _commandCenterMetadataService.Read(fullProjectPath));
    }

    private static (string? ProjectName, int AppliedTemplateCount) ReadFabricatorState(string projectPath)
    {
        var statePath = Path.Combine(projectPath, FabricatorProjectStateContract.StateRelativePath);
        if (!File.Exists(statePath))
        {
            return (null, 0);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(statePath));
            var root = document.RootElement;
            var projectName = root.TryGetProperty("project", out var project) &&
                              project.ValueKind == JsonValueKind.Object &&
                              project.TryGetProperty("name", out var name) &&
                              name.ValueKind == JsonValueKind.String
                ? name.GetString()
                : null;
            var appliedTemplateCount = root.TryGetProperty("appliedTemplates", out var appliedTemplates) &&
                                       appliedTemplates.ValueKind == JsonValueKind.Array
                ? appliedTemplates.GetArrayLength()
                : 0;

            return (projectName, appliedTemplateCount);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return (null, 0);
        }
    }

    private static (string? PackageName, string? Version) ReadPackageJson(string projectPath)
    {
        var packagePath = Path.Combine(projectPath, "package.json");
        if (!File.Exists(packagePath))
        {
            return (null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(packagePath));
            var root = document.RootElement;

            return (
                ReadString(root, "name"),
                ReadString(root, "version"));
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return (null, null);
        }
    }

    private static string? ReadAppJsonName(string projectPath)
    {
        var appJsonPath = Path.Combine(projectPath, "app.json");
        if (!File.Exists(appJsonPath))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(appJsonPath));
            var root = document.RootElement;

            if (root.TryGetProperty("displayName", out var displayName) &&
                displayName.ValueKind == JsonValueKind.String)
            {
                return displayName.GetString();
            }

            return ReadString(root, "name");
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static WorkspaceProjectStatistics ReadStatistics(string projectPath)
    {
        return new WorkspaceProjectStatistics(
            CountFiles(Path.Combine(projectPath, "src")),
            CountFiles(Path.Combine(projectPath, "src", "screens")),
            CountFiles(Path.Combine(projectPath, "src", "components")),
            CountFiles(Path.Combine(projectPath, "src", "services")),
            CountFiles(Path.Combine(projectPath, "src", "utils")));
    }

    private static int CountFiles(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return 0;
        }
    }

    private static WorkspaceStoreMetadata ReadStoreMetadata(string projectPath, string? packageVersion)
    {
        var infoPlistPath = FindFirstFile(Path.Combine(projectPath, "ios"), "Info.plist");
        var manifestPath = Path.Combine(projectPath, "android", "app", "src", "main", "AndroidManifest.xml");
        var buildGradlePath = FindFirstExistingPath(
            Path.Combine(projectPath, "android", "app", "build.gradle"),
            Path.Combine(projectPath, "android", "app", "build.gradle.kts"));

        var appStoreName = ReadPlistString(infoPlistPath, "CFBundleDisplayName") ??
                           ReadPlistString(infoPlistPath, "CFBundleName");
        var iosBundleIdentifier = ReadPlistString(infoPlistPath, "CFBundleIdentifier");
        var iosVersion = ReadPlistString(infoPlistPath, "CFBundleShortVersionString");
        var iosBuildNumber = ReadPlistString(infoPlistPath, "CFBundleVersion");
        var androidManifest = ReadAndroidManifest(manifestPath);
        var androidBuild = ReadAndroidBuildFile(buildGradlePath);

        return new WorkspaceStoreMetadata(
            appStoreName,
            androidManifest.PlayStoreName,
            iosBundleIdentifier,
            androidBuild.ApplicationId ?? androidManifest.PackageName,
            androidBuild.VersionName ?? iosVersion ?? packageVersion,
            androidBuild.VersionCode ?? iosBuildNumber);
    }

    private static string? FindFirstFile(string rootPath, string fileName)
    {
        if (!Directory.Exists(rootPath))
        {
            return null;
        }

        try
        {
            return Directory
                .EnumerateFiles(rootPath, fileName, SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .FirstOrDefault();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static string? FindFirstExistingPath(params string[] paths)
    {
        return paths.FirstOrDefault(File.Exists);
    }

    private static string? ReadPlistString(string? plistPath, string key)
    {
        if (string.IsNullOrWhiteSpace(plistPath) || !File.Exists(plistPath))
        {
            return null;
        }

        try
        {
            var document = XDocument.Load(plistPath);
            var elements = document
                .Descendants("dict")
                .Elements()
                .ToArray();

            for (var index = 0; index < elements.Length - 1; index++)
            {
                if (elements[index].Name.LocalName == "key" &&
                    string.Equals(elements[index].Value, key, StringComparison.Ordinal) &&
                    elements[index + 1].Name.LocalName == "string")
                {
                    return elements[index + 1].Value;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Xml.XmlException)
        {
            return null;
        }

        return null;
    }

    private static (string? PackageName, string? PlayStoreName) ReadAndroidManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return (null, null);
        }

        try
        {
            var document = XDocument.Load(manifestPath);
            var root = document.Root;
            XNamespace androidNamespace = "http://schemas.android.com/apk/res/android";
            var application = root?.Element("application");

            return (
                root?.Attribute("package")?.Value,
                application?.Attribute(androidNamespace + "label")?.Value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Xml.XmlException)
        {
            return (null, null);
        }
    }

    private static (string? ApplicationId, string? VersionName, string? VersionCode) ReadAndroidBuildFile(string? buildGradlePath)
    {
        if (string.IsNullOrWhiteSpace(buildGradlePath) || !File.Exists(buildGradlePath))
        {
            return (null, null, null);
        }

        try
        {
            var text = File.ReadAllText(buildGradlePath);

            return (
                ReadGradleString(text, "applicationId"),
                ReadGradleString(text, "versionName"),
                ReadGradleNumber(text, "versionCode"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return (null, null, null);
        }
    }

    private static string? ReadGradleString(string text, string key)
    {
        var match = GradleStringRegex(key).Match(text);
        return match.Success
            ? match.Groups["value"].Value
            : null;
    }

    private static string? ReadGradleNumber(string text, string key)
    {
        var match = GradleNumberRegex(key).Match(text);
        return match.Success
            ? match.Groups["value"].Value
            : null;
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static WorkspaceAgentMemory ReadAgentMemory(string projectPath)
    {
        var rootInstructionsPath = Path.Combine(projectPath, "AGENTS.md");
        var agentsDirectoryPath = Path.Combine(projectPath, ".agents");
        var handoffPath = Path.Combine(agentsDirectoryPath, "handoff.md");
        var currentFocusPath = Path.Combine(agentsDirectoryPath, "current-focus.md");

        return new WorkspaceAgentMemory(
            File.Exists(rootInstructionsPath),
            Directory.Exists(agentsDirectoryPath),
            File.Exists(handoffPath),
            File.Exists(currentFocusPath),
            ReadLastModified(handoffPath),
            ReadCurrentFocusSummary(currentFocusPath),
            CountOpenQuestions(handoffPath) + CountOpenQuestions(currentFocusPath));
    }

    private static DateTimeOffset? ReadLastModified(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static string? ReadCurrentFocusSummary(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return File
                .ReadLines(path)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !line.StartsWith('#'))
                .FirstOrDefault();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static int CountOpenQuestions(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        try
        {
            var count = 0;
            var inOpenQuestions = false;

            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();

                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inOpenQuestions = line.Contains("Open Questions", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (inOpenQuestions && line.StartsWith("- ", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return 0;
        }
    }

    private static Regex GradleStringRegex(string key)
    {
        return new Regex($"""\b{Regex.Escape(key)}\s*(?:=|\s)\s*["'](?<value>[^"']+)["']""");
    }

    private static Regex GradleNumberRegex(string key)
    {
        return new Regex($"""\b{Regex.Escape(key)}\s*(?:=|\s)\s*(?<value>\d+)""");
    }
}
