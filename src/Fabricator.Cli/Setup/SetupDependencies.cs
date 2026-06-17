using Fabricator.Core.Environment;
using Fabricator.Core.Processes;
using Fabricator.Core.Setup;

namespace Fabricator.Cli.Setup;

public static class SetupDependencies
{
    public static SetupPlanCommandHandler CreateDefaultPlanHandler(TextWriter writer)
    {
        var processRunner = new ProcessRunner();
        var systemPlatform = new SystemPlatform();
        var dependencyCheckService = new DependencyCheckService(processRunner, systemPlatform);
        var packageManagerDetector = new PackageManagerDetector(processRunner, systemPlatform);
        var setupPlanService = new SetupPlanService(
            dependencyCheckService,
            systemPlatform,
            packageManagerDetector);
        var renderer = new SetupPlanRenderer(writer);

        return new SetupPlanCommandHandler(setupPlanService, renderer);
    }
}
