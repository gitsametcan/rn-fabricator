using Fabricator.Core.Environment;
using Fabricator.Core.Processes;

namespace Fabricator.Cli.Doctor;

public static class DoctorDependencies
{
    public static DoctorCommandHandler CreateDefaultHandler(TextWriter writer)
    {
        var processRunner = new ProcessRunner();
        var dependencyCheckService = new DependencyCheckService(processRunner);
        var renderer = new DoctorSummaryRenderer(writer);

        return new DoctorCommandHandler(dependencyCheckService, renderer);
    }
}
