namespace Fabricator.Core.Workspaces;

public interface IApplicationCommandCenterMetadataService
{
    ApplicationCommandCenterMetadataReadResult Read(string projectPath);

    ApplicationCommandCenterMetadataWriteResult Write(
        string projectPath,
        ApplicationCommandCenterMetadata? metadata);
}
