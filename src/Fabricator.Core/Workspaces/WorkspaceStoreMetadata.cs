namespace Fabricator.Core.Workspaces;

public sealed record WorkspaceStoreMetadata(
    string? AppStoreName,
    string? PlayStoreName,
    string? IosBundleIdentifier,
    string? AndroidApplicationId,
    string? AppVersion,
    string? BuildNumber);
