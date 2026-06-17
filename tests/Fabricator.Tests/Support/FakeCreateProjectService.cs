using Fabricator.Core;
using Fabricator.Core.Processes;
using Fabricator.Core.Projects;

namespace Fabricator.Tests;

public sealed class FakeCreateProjectService : ICreateProjectService
{
    private readonly Queue<CreateProjectResult> _results = new();

    public List<CreateProjectRequest> Requests { get; } = [];

    public void Enqueue(CreateProjectResult result)
    {
        _results.Enqueue(result);
    }

    public Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        if (_results.Count == 0)
        {
            var validation = new CreateProjectValidator().Validate(request);
            if (!validation.IsValid)
            {
                return Task.FromResult(CreateProjectResult.Invalid(validation));
            }

            var processResult = new ProcessRunResult(
                ExitCodes.GeneralFailure,
                string.Empty,
                "No fake create project result configured.");

            return Task.FromResult(CreateProjectResult.Completed(
                validation,
                new ProcessRunRequest("npx", [], validation.FullOutputDirectory),
                processResult));
        }

        return Task.FromResult(_results.Dequeue());
    }
}
