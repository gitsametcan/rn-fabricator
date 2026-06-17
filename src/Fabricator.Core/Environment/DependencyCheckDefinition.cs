namespace Fabricator.Core.Environment;

public sealed record DependencyCheckDefinition(
    string Name,
    string FileName,
    IReadOnlyList<string> Arguments,
    string RemediationHint);
