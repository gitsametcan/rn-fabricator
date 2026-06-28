namespace Fabricator.Core.Projects;

public static class FabricatorProjectContract
{
    public const int CurrentSchemaVersion = 1;
    public const string ManifestRelativePath = ".fabricator/project.json";
    public const string ProjectKind = "fabricator-react-native-project";
    public const string ProjectType = "react-native-cli";
    public const string SourceRoot = "src";
    public const string BarrelExportIntegrationType = "barrel-export";

    public static FabricatorProjectManifest CreateManifest(string fabricatorVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fabricatorVersion);

        return new FabricatorProjectManifest(
            CurrentSchemaVersion,
            ProjectKind,
            fabricatorVersion,
            ProjectType,
            SourceRoot,
            CreateDefaultFolders(),
            CreateDefaultIntegrationPoints());
    }

    public static IReadOnlyList<FabricatorProjectFolder> CreateDefaultFolders() =>
    [
        new("app", "src/app", "Application composition and app-level wiring."),
        new("components", "src/components", "Reusable UI components."),
        new("config", "src/config", "Application configuration helpers and constants."),
        new("constants", "src/constants", "Shared static values."),
        new("hooks", "src/hooks", "Reusable React hooks."),
        new("navigation", "src/navigation", "Navigation stacks, route definitions, and navigator composition."),
        new("screens", "src/screens", "Screen-level mobile views."),
        new("services", "src/services", "API clients and external service adapters."),
        new("storage", "src/storage", "Local persistence helpers."),
        new("theme", "src/theme", "Theme tokens and styling helpers."),
        new("types", "src/types", "Shared TypeScript types."),
        new("utils", "src/utils", "Reusable utility functions.")
    ];

    public static IReadOnlyList<FabricatorProjectIntegrationPoint> CreateDefaultIntegrationPoints() =>
    [
        new("screensBarrel", "src/screens/index.ts", BarrelExportIntegrationType, "Screen exports managed by Fabricator."),
        new("componentsBarrel", "src/components/index.ts", BarrelExportIntegrationType, "Component exports managed by Fabricator."),
        new("servicesBarrel", "src/services/index.ts", BarrelExportIntegrationType, "Service exports managed by Fabricator.")
    ];
}
