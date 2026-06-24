namespace Fabricator.Core.Templates;

public sealed class TemplateSourceResolutionResult
{
    private TemplateSourceResolutionResult(
        bool succeeded,
        string? source,
        string? sourceDescription,
        string? errorMessage)
    {
        Succeeded = succeeded;
        Source = source;
        SourceDescription = sourceDescription;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }

    public string? Source { get; }

    public string? SourceDescription { get; }

    public string? ErrorMessage { get; }

    public static TemplateSourceResolutionResult Resolved(
        string source,
        string sourceDescription)
    {
        return new TemplateSourceResolutionResult(true, source, sourceDescription, null);
    }

    public static TemplateSourceResolutionResult Failed(string errorMessage)
    {
        return new TemplateSourceResolutionResult(false, null, null, errorMessage);
    }
}
