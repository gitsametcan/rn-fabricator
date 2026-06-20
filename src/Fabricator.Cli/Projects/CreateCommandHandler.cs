using Fabricator.Core.Projects;
using Fabricator.Core.Processes;

namespace Fabricator.Cli.Projects;

public sealed class CreateCommandHandler
{
    private readonly ICreateProjectService _createProjectService;
    private readonly TextWriter _errorWriter;
    private readonly TextWriter _outputWriter;

    public CreateCommandHandler(
        ICreateProjectService createProjectService,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        _createProjectService = createProjectService;
        _outputWriter = outputWriter;
        _errorWriter = errorWriter;
    }

    public async Task<int> RunAsync(
        string name,
        string template,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var commandRendered = false;
        var streamedProcessOutput = false;
        var result = await _createProjectService.CreateAsync(
            new CreateProjectRequest(
                name,
                template,
                outputDirectory,
                command =>
                {
                    commandRendered = true;
                    RenderCommand(command);
                },
                output =>
                {
                    streamedProcessOutput = true;
                    _outputWriter.Write(output);
                },
                error =>
                {
                    streamedProcessOutput = true;
                    _errorWriter.Write(error);
                }),
            cancellationToken);

        if (!result.Validation.IsValid)
        {
            RenderInvalidInput(result.Validation);
            return result.ExitCode;
        }

        if (!commandRendered && result.Command is not null)
        {
            RenderCommand(result.Command);
        }

        if (!result.Succeeded)
        {
            RenderProcessFailure(result, streamedProcessOutput);
            return result.ExitCode;
        }

        RenderSuccess(result);
        return result.ExitCode;
    }

    private void RenderInvalidInput(CreateProjectValidationResult validation)
    {
        _errorWriter.WriteLine("Invalid create command input:");

        foreach (var error in validation.Errors)
        {
            _errorWriter.WriteLine($"- {error}");
        }
    }

    private void RenderCommand(ProcessRunRequest command)
    {
        _outputWriter.WriteLine($"Creating React Native project: {command.Arguments.LastOrDefault()}");

        if (!string.IsNullOrWhiteSpace(command.WorkingDirectory))
        {
            _outputWriter.WriteLine($"Output directory: {command.WorkingDirectory}");
        }

        _outputWriter.WriteLine($"Command: {command.FileName} {string.Join(' ', command.Arguments)}");
    }

    private void RenderSuccess(CreateProjectResult result)
    {
        var projectName = result.Validation.Request.ProjectName;

        _outputWriter.WriteLine($"React Native project created: {result.ProjectPath}");
        _outputWriter.WriteLine();
        _outputWriter.WriteLine("Next steps:");
        _outputWriter.WriteLine($"1. cd {projectName}");
        _outputWriter.WriteLine("2. npm start");
        _outputWriter.WriteLine("3. npm run ios");
        _outputWriter.WriteLine("4. npm run android");
        _outputWriter.WriteLine();
        _outputWriter.WriteLine($"Template selected: {result.Validation.Request.TemplateName}");
        _outputWriter.WriteLine("Example config files: not generated yet; basic-auth template files are planned for v0.4.0.");
    }

    private void RenderProcessFailure(CreateProjectResult result, bool processOutputAlreadyWritten)
    {
        _errorWriter.WriteLine("React Native project creation failed.");

        if (result.ProcessResult is null || processOutputAlreadyWritten)
        {
            RenderRollback(result.Rollback);
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.ProcessResult.StandardOutput))
        {
            _errorWriter.WriteLine(result.ProcessResult.StandardOutput.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.ProcessResult.StandardError))
        {
            _errorWriter.WriteLine(result.ProcessResult.StandardError.Trim());
        }

        RenderRollback(result.Rollback);
    }

    private void RenderRollback(CreateProjectRollbackResult rollback)
    {
        if (!rollback.Attempted)
        {
            _errorWriter.WriteLine($"Rollback: {rollback.Message}");
            return;
        }

        if (rollback.Succeeded)
        {
            _errorWriter.WriteLine($"Rollback completed: {rollback.Message}");
            return;
        }

        _errorWriter.WriteLine($"Rollback failed: {rollback.Message}");

        if (!string.IsNullOrWhiteSpace(rollback.ErrorMessage))
        {
            _errorWriter.WriteLine(rollback.ErrorMessage);
        }
    }
}
