using Fabricator.Core.Processes;

namespace Fabricator.Core.Projects;

public sealed record CreateProjectRequest(
    string ProjectName,
    string TemplateName,
    string OutputDirectory,
    Action<ProcessRunRequest>? OnCommandPrepared = null,
    Action<string>? OnStandardOutput = null,
    Action<string>? OnStandardError = null,
    string? TemplateSource = null,
    bool InstallPods = false);
