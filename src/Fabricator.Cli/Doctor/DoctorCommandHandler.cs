using Fabricator.Core.Environment;

namespace Fabricator.Cli.Doctor;

public sealed class DoctorCommandHandler
{
    private readonly IDependencyCheckService _dependencyCheckService;
    private readonly DoctorSummaryRenderer _renderer;

    public DoctorCommandHandler(
        IDependencyCheckService dependencyCheckService,
        DoctorSummaryRenderer renderer)
    {
        _dependencyCheckService = dependencyCheckService;
        _renderer = renderer;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var summaries = new[]
        {
            await _dependencyCheckService.CheckCoreToolsAsync(cancellationToken),
            await _dependencyCheckService.CheckAppleToolsAsync(cancellationToken),
            await _dependencyCheckService.CheckAndroidToolsAsync(cancellationToken)
        };

        var results = summaries
            .SelectMany(summary => summary.Results)
            .ToArray();

        var summary = new DependencyCheckSummary(results);
        _renderer.Render(summary);

        return summary.ExitCode;
    }
}
