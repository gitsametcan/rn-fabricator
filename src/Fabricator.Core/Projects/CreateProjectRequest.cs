namespace Fabricator.Core.Projects;

public sealed record CreateProjectRequest(
    string ProjectName,
    string TemplateName,
    string OutputDirectory);
