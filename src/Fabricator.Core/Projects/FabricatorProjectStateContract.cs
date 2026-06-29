namespace Fabricator.Core.Projects;

public static class FabricatorProjectStateContract
{
    public const int CurrentSchemaVersion = 1;
    public const string StateRelativePath = "fabricator.json";
    public const string ProjectStateKind = "fabricator-project-state";
    public const string ProjectStateOperationCreate = "create";
    public const string ProjectStateResultApplied = "applied";
    public const string SourceTypeEmbedded = "embedded";
    public const string SourceTypeLocal = "local";
    public const string SourceTypeRemote = "remote";

    public static FabricatorProjectState CreateInitialState(
        string projectName,
        string toolVersion,
        string starterId,
        string starterVersion,
        string starterCategory,
        string? templateSource,
        IReadOnlyList<string> writtenFiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(starterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(starterVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(starterCategory);

        var source = CreateTemplateSource(starterId, templateSource);
        var createdAt = DateTimeOffset.UtcNow;

        return new FabricatorProjectState(
            CurrentSchemaVersion,
            ProjectStateKind,
            toolVersion,
            new FabricatorProjectStateMetadata(
                projectName,
                FabricatorProjectContract.ProjectType,
                createdAt),
            [source],
            [
                new FabricatorAppliedTemplateState(
                    starterId,
                    starterVersion,
                    starterCategory,
                    source,
                    createdAt,
                    ProjectStateOperationCreate,
                    ProjectStateResultApplied,
                    new FabricatorTemplateFileState(writtenFiles, [], []),
                    [],
                    [])
            ]);
    }

    private static FabricatorTemplateSourceState CreateTemplateSource(
        string starterId,
        string? templateSource)
    {
        if (string.IsNullOrWhiteSpace(templateSource))
        {
            return new FabricatorTemplateSourceState(
                "embedded",
                SourceTypeEmbedded,
                starterId,
                true);
        }

        var sourceType = IsRemoteSource(templateSource)
            ? SourceTypeRemote
            : SourceTypeLocal;
        var sourceName = sourceType == SourceTypeRemote
            ? "remote"
            : "local";

        return new FabricatorTemplateSourceState(
            sourceName,
            sourceType,
            templateSource,
            true);
    }

    private static bool IsRemoteSource(string templateSource)
    {
        return Uri.TryCreate(templateSource, UriKind.Absolute, out var uri) &&
               (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }
}
