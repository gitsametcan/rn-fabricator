using Fabricator.Core.Environment;
using Fabricator.Core.Processes;
using Fabricator.Core.Setup;
using Fabricator.Core.Toolchains;

namespace Fabricator.Cli.Setup;

public static class SetupDependencies
{
    public static SetupPlanCommandHandler CreateDefaultPlanHandler(TextWriter writer)
    {
        var processRunner = new ProcessRunner();
        var systemPlatform = new SystemPlatform();
        var dependencyCheckService = new DependencyCheckService(processRunner, systemPlatform);
        var packageManagerDetector = new PackageManagerDetector(processRunner, systemPlatform);
        var toolchainProfileProvider = new LocalToolchainProfileProvider();
        var setupPlanService = new SetupPlanService(
            dependencyCheckService,
            systemPlatform,
            packageManagerDetector,
            toolchainProfileProvider);
        var renderer = new SetupPlanRenderer(writer);

        return new SetupPlanCommandHandler(setupPlanService, renderer, Console.Error);
    }
}
