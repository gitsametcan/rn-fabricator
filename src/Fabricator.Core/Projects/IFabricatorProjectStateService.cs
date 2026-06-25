namespace Fabricator.Core.Projects;

public interface IFabricatorProjectStateService
{
    FabricatorProjectStateUpdateResult ValidateCanTrack(string projectDirectory);

    Task<FabricatorProjectStateUpdateResult> TrackApplyAsync(
        FabricatorTemplateApplyStateTrackingRequest request,
        CancellationToken cancellationToken = default);
}
