using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fabricator.Core.Toolchains;

public sealed class LocalToolchainProfileProvider : IToolchainProfileProvider
{
    public const string DefaultProfilesDirectoryName = "ToolchainProfiles";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _profilesRootDirectory;
    private readonly ToolchainProfileValidator _validator;

    private IReadOnlyList<ToolchainProfile>? _profiles;

    public LocalToolchainProfileProvider()
        : this(Path.Combine(AppContext.BaseDirectory, DefaultProfilesDirectoryName))
    {
    }

    public LocalToolchainProfileProvider(
        string profilesRootDirectory,
        ToolchainProfileValidator? validator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profilesRootDirectory);
        _profilesRootDirectory = Path.GetFullPath(profilesRootDirectory);
        _validator = validator ?? new ToolchainProfileValidator();
    }

    public IReadOnlyList<ToolchainProfile> GetProfiles()
    {
        _profiles ??= LoadProfiles();
        return _profiles;
    }

    public ToolchainProfileLookupResult GetDefaultProfile()
    {
        var defaultProfiles = GetProfiles()
            .Where(profile => profile.IsDefault)
            .ToArray();

        return defaultProfiles.Length switch
        {
            1 => ToolchainProfileLookupResult.Found(defaultProfiles[0]),
            0 => ToolchainProfileLookupResult.Failed("No default toolchain profile was found."),
            _ => ToolchainProfileLookupResult.Failed("Multiple default toolchain profiles were found.")
        };
    }

    public ToolchainProfileLookupResult GetProfile(string profileIdOrReactNativeVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileIdOrReactNativeVersion);

        var profile = GetProfiles().FirstOrDefault(candidate =>
            string.Equals(candidate.Id, profileIdOrReactNativeVersion, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate.ReactNativeVersion, profileIdOrReactNativeVersion, StringComparison.OrdinalIgnoreCase));

        return profile is null
            ? ToolchainProfileLookupResult.Failed($"Unsupported React Native toolchain profile: {profileIdOrReactNativeVersion}.")
            : ToolchainProfileLookupResult.Found(profile);
    }

    private IReadOnlyList<ToolchainProfile> LoadProfiles()
    {
        if (!Directory.Exists(_profilesRootDirectory))
        {
            throw new ToolchainProfileProviderException($"Toolchain profile directory was not found: {_profilesRootDirectory}");
        }

        var profilePaths = Directory.GetFiles(_profilesRootDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (profilePaths.Length == 0)
        {
            throw new ToolchainProfileProviderException($"No toolchain profile files were found in: {_profilesRootDirectory}");
        }

        var profiles = new List<ToolchainProfile>();

        foreach (var profilePath in profilePaths)
        {
            profiles.Add(LoadProfile(profilePath));
        }

        ValidateUniqueProfiles(profiles);

        return profiles;
    }

    private ToolchainProfile LoadProfile(string profilePath)
    {
        ToolchainProfile? profile;

        try
        {
            profile = JsonSerializer.Deserialize<ToolchainProfile>(
                File.ReadAllText(profilePath),
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new ToolchainProfileProviderException(
                $"Toolchain profile could not be read: {profilePath}",
                exception);
        }

        var validation = _validator.Validate(profile);

        if (!validation.IsValid)
        {
            throw new ToolchainProfileProviderException(
                $"Toolchain profile is invalid: {profilePath}{System.Environment.NewLine}{string.Join(System.Environment.NewLine, validation.Errors)}");
        }

        return profile!;
    }

    private static void ValidateUniqueProfiles(IReadOnlyList<ToolchainProfile> profiles)
    {
        AddDuplicateProfileErrors(
            profiles,
            profile => profile.Id,
            "Duplicate toolchain profile id");
        AddDuplicateProfileErrors(
            profiles,
            profile => profile.ReactNativeVersion,
            "Duplicate React Native toolchain version");
    }

    private static void AddDuplicateProfileErrors(
        IReadOnlyList<ToolchainProfile> profiles,
        Func<ToolchainProfile, string> valueSelector,
        string errorPrefix)
    {
        var duplicateValue = profiles
            .GroupBy(valueSelector, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateValue is not null)
        {
            throw new ToolchainProfileProviderException($"{errorPrefix}: {duplicateValue}");
        }
    }
}
