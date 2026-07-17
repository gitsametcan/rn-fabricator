using System.Text.Json;

namespace Fabricator.Desktop.WorkspaceSettings;

public sealed class FileWorkspaceSettingsStore : IWorkspaceSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public FileWorkspaceSettingsStore()
        : this(GetDefaultSettingsPath())
    {
    }

    public FileWorkspaceSettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    public string? LoadLastWorkspacePath()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return null;
            }

            var settings = JsonSerializer.Deserialize<WorkspaceSettings>(
                File.ReadAllText(_settingsPath),
                JsonOptions);

            return string.IsNullOrWhiteSpace(settings?.LastWorkspacePath)
                ? null
                : settings.LastWorkspacePath;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    public void SaveLastWorkspacePath(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var settings = new WorkspaceSettings(Path.GetFullPath(workspacePath));
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDirectory = string.IsNullOrWhiteSpace(appData)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : appData;

        return Path.Combine(baseDirectory, "rn-fabricator", "desktop-settings.json");
    }

    private sealed record WorkspaceSettings(string LastWorkspacePath);
}
