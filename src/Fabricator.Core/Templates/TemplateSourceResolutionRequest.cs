namespace Fabricator.Core.Templates;

public sealed record TemplateSourceResolutionRequest(
    string? ExplicitSource,
    string WorkingDirectory,
    string? ProjectDirectory = null);
