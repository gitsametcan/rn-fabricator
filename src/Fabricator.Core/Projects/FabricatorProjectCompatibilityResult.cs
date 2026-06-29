namespace Fabricator.Core.Projects;

public sealed class FabricatorProjectCompatibilityResult
{
    private FabricatorProjectCompatibilityResult(
        string projectDirectory,
        FabricatorProjectManifest? manifest,
        IReadOnlyList<string> errors)
    {
        ProjectDirectory = projectDirectory;
        Manifest = manifest;
        Errors = errors;
    }

    public string ProjectDirectory { get; }

    public FabricatorProjectManifest? Manifest { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsCompatible => Errors.Count == 0;

    public static FabricatorProjectCompatibilityResult Compatible(
        string projectDirectory,
        FabricatorProjectManifest manifest)
    {
        return new FabricatorProjectCompatibilityResult(projectDirectory, manifest, []);
    }

    public static FabricatorProjectCompatibilityResult Incompatible(
        string projectDirectory,
        FabricatorProjectManifest? manifest,
        IReadOnlyList<string> errors)
    {
        return new FabricatorProjectCompatibilityResult(projectDirectory, manifest, errors);
    }
}
