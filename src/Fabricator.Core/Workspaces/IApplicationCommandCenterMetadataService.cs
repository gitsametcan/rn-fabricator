namespace Fabricator.Core.Workspaces;

public interface IApplicationCommandCenterMetadataService
{
    ApplicationCommandCenterMetadataReadResult Read(string projectPath);
}
