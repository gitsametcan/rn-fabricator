using Fabricator.Core.Environment;
using Fabricator.Core.Processes;
using Fabricator.Core.Setup;
using Fabricator.Core.Toolchains;

namespace Fabricator.Cli.Setup;

public static class SetupDependencies
{
    public static SetupPlanCommandHandler CreateDefaultPlanHandler(TextWriter writer)
    {
        var setupPlanService = CreateDefaultPlanService();
        var renderer = new SetupPlanRenderer(writer);

        return new SetupPlanCommandHandler(setupPlanService, renderer, Console.Error);
    }

    public static SetupApplyCommandHandler CreateDefaultApplyHandler(
        TextReader reader,
        TextWriter writer)
    {
        var setupPlanService = CreateDefaultPlanService();
        var processRunner = new ProcessRunner();
        var renderer = new SetupPlanRenderer(writer);

        return new SetupApplyCommandHandler(
            setupPlanService,
            renderer,
            processRunner,
            reader,
            writer,
            Console.Error);
    }

    private static SetupPlanService CreateDefaultPlanService()
    {
        var processRunner = new ProcessRunner();
        var systemPlatform = new SystemPlatform();
        var dependencyCheckService = new DependencyCheckService(processRunner, systemPlatform);
        var packageManagerDetector = new PackageManagerDetector(processRunner, systemPlatform);
        var toolchainProfileProvider = new LocalToolchainProfileProvider();

        return new SetupPlanService(
            dependencyCheckService,
            systemPlatform,
            packageManagerDetector,
            toolchainProfileProvider);
    }
}
