using Fabricator.Core.Processes;

namespace Fabricator.Core.Projects;

public sealed class CreateProjectResult
{
    private CreateProjectResult(
        CreateProjectValidationResult validation,
        ProcessRunRequest? command,
        ProcessRunResult? processResult)
    {
        Validation = validation;
        Command = command;
        ProcessResult = processResult;
    }

    public CreateProjectValidationResult Validation { get; }

    public ProcessRunRequest? Command { get; }

    public ProcessRunResult? ProcessResult { get; }

    public bool Succeeded => Validation.IsValid && ProcessResult?.Succeeded == true;

    public int ExitCode
    {
        get
        {
            if (!Validation.IsValid)
            {
                return ExitCodes.InvalidInput;
            }

            return ProcessResult?.ExitCode ?? ExitCodes.GeneralFailure;
        }
    }

    public string ProjectPath => Validation.FullProjectPath;

    public static CreateProjectResult Invalid(CreateProjectValidationResult validation)
    {
        return new CreateProjectResult(validation, null, null);
    }

    public static CreateProjectResult Completed(
        CreateProjectValidationResult validation,
        ProcessRunRequest command,
        ProcessRunResult processResult)
    {
        return new CreateProjectResult(validation, command, processResult);
    }
}
