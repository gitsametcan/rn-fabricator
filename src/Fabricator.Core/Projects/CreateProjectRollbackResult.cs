namespace Fabricator.Core.Projects;

public sealed class CreateProjectRollbackResult
{
    private CreateProjectRollbackResult(
        bool attempted,
        bool succeeded,
        string message,
        string? errorMessage = null)
    {
        Attempted = attempted;
        Succeeded = succeeded;
        Message = message;
        ErrorMessage = errorMessage;
    }

    public bool Attempted { get; }

    public bool Succeeded { get; }

    public string Message { get; }

    public string? ErrorMessage { get; }

    public static CreateProjectRollbackResult NotRequired(string message)
    {
        return new CreateProjectRollbackResult(false, false, message);
    }

    public static CreateProjectRollbackResult Completed(string message)
    {
        return new CreateProjectRollbackResult(true, true, message);
    }

    public static CreateProjectRollbackResult Failed(string message, string errorMessage)
    {
        return new CreateProjectRollbackResult(true, false, message, errorMessage);
    }
}
