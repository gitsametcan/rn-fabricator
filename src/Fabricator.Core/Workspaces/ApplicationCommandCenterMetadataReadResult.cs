namespace Fabricator.Core.Workspaces;

public sealed record ApplicationCommandCenterMetadataReadResult(
    string ProjectPath,
    string MetadataPath,
    bool Exists,
    bool IsValid,
    ApplicationCommandCenterMetadata Metadata,
    IReadOnlyList<string> Errors)
{
    public bool HasMetadata => Exists && IsValid;

    public static ApplicationCommandCenterMetadataReadResult Missing(
        string projectPath,
        string metadataPath)
    {
        return new ApplicationCommandCenterMetadataReadResult(
            projectPath,
            metadataPath,
            Exists: false,
            IsValid: true,
            ApplicationCommandCenterMetadata.Empty,
            []);
    }

    public static ApplicationCommandCenterMetadataReadResult Loaded(
        string projectPath,
        string metadataPath,
        ApplicationCommandCenterMetadata metadata)
    {
        return new ApplicationCommandCenterMetadataReadResult(
            projectPath,
            metadataPath,
            Exists: true,
            IsValid: true,
            metadata,
            []);
    }

    public static ApplicationCommandCenterMetadataReadResult Invalid(
        string projectPath,
        string metadataPath,
        IReadOnlyList<string> errors)
    {
        return new ApplicationCommandCenterMetadataReadResult(
            projectPath,
            metadataPath,
            Exists: true,
            IsValid: false,
            ApplicationCommandCenterMetadata.Empty,
            errors);
    }
}
