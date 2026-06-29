using System.Diagnostics;
using System.Text;

namespace Fabricator.Core.Processes;

public sealed class ProcessRunner : IProcessRunner
{
    private const int BufferSize = 4096;

    public async Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);

        using var process = new Process();
        process.StartInfo.FileName = request.FileName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
        {
            process.StartInfo.WorkingDirectory = request.WorkingDirectory;
        }

        foreach (var argument in request.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ProcessRunResult(
                ExitCodes.GeneralFailure,
                string.Empty,
                ex.Message);
        }

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        var standardOutputTask = ReadStreamAsync(
            process.StandardOutput,
            standardOutput,
            request.OnStandardOutput,
            cancellationToken);
        var standardErrorTask = ReadStreamAsync(
            process.StandardError,
            standardError,
            request.OnStandardError,
            cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        await standardOutputTask;
        await standardErrorTask;

        return new ProcessRunResult(
            process.ExitCode,
            standardOutput.ToString(),
            standardError.ToString());
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        StringBuilder output,
        Action<string>? onData,
        CancellationToken cancellationToken)
    {
        var buffer = new char[BufferSize];

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                return;
            }

            var chunk = new string(buffer, 0, read);
            output.Append(chunk);
            onData?.Invoke(chunk);
        }
    }
}
