namespace Fabricator.Core.Setup;

public sealed record SetupPlanToolchainProfile(
    string Id,
    string DisplayName,
    string ReactNativeVersion,
    bool IsDefault);
