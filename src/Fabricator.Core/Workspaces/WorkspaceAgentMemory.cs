namespace Fabricator.Core.Workspaces;

public sealed record WorkspaceAgentMemory(
    bool HasRootInstructions,
    bool HasAgentsDirectory,
    bool HasHandoff,
    bool HasCurrentFocus,
    DateTimeOffset? HandoffLastModified,
    string? CurrentFocusSummary,
    int OpenQuestionCount);
