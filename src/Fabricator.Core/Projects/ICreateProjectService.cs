namespace Fabricator.Core.Projects;

public interface ICreateProjectService
{
    Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default);
}
