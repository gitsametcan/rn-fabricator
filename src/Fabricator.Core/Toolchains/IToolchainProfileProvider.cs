namespace Fabricator.Core.Toolchains;

public interface IToolchainProfileProvider
{
    IReadOnlyList<ToolchainProfile> GetProfiles();

    ToolchainProfileLookupResult GetDefaultProfile();

    ToolchainProfileLookupResult GetProfile(string profileIdOrReactNativeVersion);
}
