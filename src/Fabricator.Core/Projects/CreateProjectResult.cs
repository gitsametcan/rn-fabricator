using Fabricator.Core.Processes;

namespace Fabricator.Core.Projects;

public sealed class CreateProjectResult
{
    private CreateProjectResult(
        CreateProjectValidationResult validation,
        ProcessRunRequest? command,
        ProcessRunResult? processResult,
        CreateProjectRollbackResult rollback)
    {
        Validation = validation;
        Command = command;
        ProcessResult = processResult;
        Rollback = rollback;
    }

    public CreateProjectValidationResult Validation { get; }

    public ProcessRunRequest? Command { get; }

    public ProcessRunResult? ProcessResult { get; }

    public CreateProjectRollbackResult Rollback { get; }

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
        return new CreateProjectResult(
            validation,
            null,
            null,
            CreateProjectRollbackResult.NotRequired("Rollback was not required because validation failed before project creation."));
    }

    public static CreateProjectResult Completed(
        CreateProjectValidationResult validation,
        ProcessRunRequest command,
        ProcessRunResult processResult)
    {
        return Completed(
            validation,
            command,
            processResult,
            CreateProjectRollbackResult.NotRequired("Rollback was not required."));
    }

    public static CreateProjectResult Completed(
        CreateProjectValidationResult validation,
        ProcessRunRequest command,
        ProcessRunResult processResult,
        CreateProjectRollbackResult rollback)
    {
        return new CreateProjectResult(validation, command, processResult, rollback);
    }
}
