namespace Fabricator.Core.Toolchains;

public sealed record ToolchainProfile(
    string Id,
    string DisplayName,
    string ReactNativeVersion,
    bool IsDefault,
    ToolchainRequirement NodeJs,
    ToolchainRequirement Java,
    ToolchainRequirement Xcode,
    ToolchainRequirement CocoaPods,
    ToolchainRequirement Watchman,
    AndroidToolchainRequirement Android);
