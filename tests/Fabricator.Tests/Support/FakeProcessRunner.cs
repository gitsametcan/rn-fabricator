using Fabricator.Core;
using Fabricator.Core.Processes;

namespace Fabricator.Tests;

public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Queue<ProcessRunResult> _results = new();

    public List<ProcessRunRequest> Requests { get; } = [];

    public Action<ProcessRunRequest>? OnRun { get; set; }

    public void Enqueue(ProcessRunResult result)
    {
        _results.Enqueue(result);
    }

    public Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        OnRun?.Invoke(request);

        if (_results.Count == 0)
        {
            return Task.FromResult(new ProcessRunResult(ExitCodes.GeneralFailure, string.Empty, "No fake result configured."));
        }

        var result = _results.Dequeue();
        request.OnStandardOutput?.Invoke(result.StandardOutput);
        request.OnStandardError?.Invoke(result.StandardError);

        return Task.FromResult(result);
    }
}
