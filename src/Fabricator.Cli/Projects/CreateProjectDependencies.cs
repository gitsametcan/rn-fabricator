using Fabricator.Core.Processes;
using Fabricator.Core.Projects;

namespace Fabricator.Cli.Projects;

public static class CreateProjectDependencies
{
    public static CreateCommandHandler CreateDefaultHandler(
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        var processRunner = new ProcessRunner();
        var validator = new CreateProjectValidator();
        var createProjectService = new CreateProjectService(validator, processRunner);

        return new CreateCommandHandler(createProjectService, outputWriter, errorWriter);
    }
}
