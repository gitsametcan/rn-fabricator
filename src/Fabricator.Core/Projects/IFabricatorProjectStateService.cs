namespace Fabricator.Core.Projects;

public interface IFabricatorProjectStateService
{
    FabricatorProjectStateUpdateResult ValidateCanTrack(string projectDirectory);

    Task<FabricatorProjectTemplateStatusResult> GetTemplateStatusAsync(
        string projectDirectory,
        CancellationToken cancellationToken = default);

    Task<FabricatorProjectStateUpdateResult> TrackApplyAsync(
        FabricatorTemplateApplyStateTrackingRequest request,
        CancellationToken cancellationToken = default);
}
