namespace Fabricator.Core.Workspaces;

public sealed record ApplicationCommandCenterMetadataWriteResult(
    string ProjectPath,
    string MetadataPath,
    bool IsSuccess,
    ApplicationCommandCenterMetadata Metadata,
    IReadOnlyList<string> Errors)
{
    public static ApplicationCommandCenterMetadataWriteResult Written(
        string projectPath,
        string metadataPath,
        ApplicationCommandCenterMetadata metadata)
    {
        return new ApplicationCommandCenterMetadataWriteResult(
            projectPath,
            metadataPath,
            IsSuccess: true,
            metadata,
            []);
    }

    public static ApplicationCommandCenterMetadataWriteResult Failed(
        string projectPath,
        string metadataPath,
        IReadOnlyList<string> errors)
    {
        return new ApplicationCommandCenterMetadataWriteResult(
            projectPath,
            metadataPath,
            IsSuccess: false,
            ApplicationCommandCenterMetadata.Empty,
            errors);
    }
}
