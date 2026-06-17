using Fabricator.Core.Processes;

namespace Fabricator.Core.Projects;

public sealed class CreateProjectService : ICreateProjectService
{
    private static readonly string[] ReactNativeCliArguments = ["@react-native-community/cli@latest", "init"];

    private readonly IProcessRunner _processRunner;
    private readonly CreateProjectValidator _validator;

    public CreateProjectService(
        CreateProjectValidator validator,
        IProcessRunner processRunner)
    {
        _validator = validator;
        _processRunner = processRunner;
    }

    public async Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);

        if (!validation.IsValid)
        {
            return CreateProjectResult.Invalid(validation);
        }

        var command = new ProcessRunRequest(
            "npx",
            [.. ReactNativeCliArguments, validation.Request.ProjectName],
            validation.FullOutputDirectory);
        var processResult = await _processRunner.RunAsync(command, cancellationToken);

        return CreateProjectResult.Completed(validation, command, processResult);
    }
}
