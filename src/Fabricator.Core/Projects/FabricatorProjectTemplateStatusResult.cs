namespace Fabricator.Core.Projects;

public sealed class FabricatorProjectTemplateStatusResult
{
    private FabricatorProjectTemplateStatusResult(
        string projectDirectory,
        IReadOnlyList<FabricatorAppliedTemplateStatus> appliedTemplates,
        IReadOnlyList<string> errors)
    {
        ProjectDirectory = projectDirectory;
        AppliedTemplates = appliedTemplates;
        Errors = errors;
    }

    public string ProjectDirectory { get; }

    public IReadOnlyList<FabricatorAppliedTemplateStatus> AppliedTemplates { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static FabricatorProjectTemplateStatusResult Success(
        string projectDirectory,
        IReadOnlyList<FabricatorAppliedTemplateStatus> appliedTemplates)
    {
        return new FabricatorProjectTemplateStatusResult(projectDirectory, appliedTemplates, []);
    }

    public static FabricatorProjectTemplateStatusResult Failed(
        string projectDirectory,
        IReadOnlyList<string> errors)
    {
        return new FabricatorProjectTemplateStatusResult(projectDirectory, [], errors);
    }
}
