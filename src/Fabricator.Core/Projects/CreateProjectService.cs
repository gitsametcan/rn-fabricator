using Fabricator.Core.Processes;

namespace Fabricator.Core.Projects;

public sealed class CreateProjectService : ICreateProjectService
{
    private static readonly string[] ReactNativeCliArguments = ["@react-native-community/cli@latest", "init"];

    private readonly IProjectFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;
    private readonly CreateProjectValidator _validator;

    public CreateProjectService(
        CreateProjectValidator validator,
        IProcessRunner processRunner)
        : this(validator, processRunner, new SystemProjectFileSystem())
    {
    }

    public CreateProjectService(
        CreateProjectValidator validator,
        IProcessRunner processRunner,
        IProjectFileSystem fileSystem)
    {
        _validator = validator;
        _processRunner = processRunner;
        _fileSystem = fileSystem;
    }

    public async Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);

        if (!validation.IsValid)
        {
            return CreateProjectResult.Invalid(validation);
        }

        var command = new ProcessRunRequest(
            "npx",
            [.. ReactNativeCliArguments, validation.Request.ProjectName],
            validation.FullOutputDirectory);
        var processResult = await _processRunner.RunAsync(command, cancellationToken);
        var rollback = processResult.Succeeded
            ? CreateProjectRollbackResult.NotRequired("Rollback was not required because project creation succeeded.")
            : RollBackPartialProject(validation);

        return CreateProjectResult.Completed(validation, command, processResult, rollback);
    }

    private CreateProjectRollbackResult RollBackPartialProject(CreateProjectValidationResult validation)
    {
        var projectPath = validation.FullProjectPath;

        if (!IsSafeGeneratedProjectPath(validation.FullOutputDirectory, projectPath))
        {
            return CreateProjectRollbackResult.Failed(
                "Rollback was skipped because the target path was outside the output directory.",
                projectPath);
        }

        if (!_fileSystem.DirectoryExists(projectPath))
        {
            if (_fileSystem.FileExists(projectPath))
            {
                return CreateProjectRollbackResult.Failed(
                    "Rollback was skipped because the target path is a file, not a generated project directory.",
                    projectPath);
            }

            return CreateProjectRollbackResult.NotRequired("Rollback was not required because no partial project directory was found.");
        }

        try
        {
            _fileSystem.DeleteDirectory(projectPath, recursive: true);

            return _fileSystem.DirectoryExists(projectPath)
                ? CreateProjectRollbackResult.Failed(
                    "Rollback attempted to remove the partial project directory, but it still exists.",
                    projectPath)
                : CreateProjectRollbackResult.Completed($"Removed partial project directory: {projectPath}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CreateProjectRollbackResult.Failed(
                "Rollback failed while removing the partial project directory.",
                exception.Message);
        }
    }

    private static bool IsSafeGeneratedProjectPath(string outputDirectory, string projectPath)
    {
        var fullOutputDirectory = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var fullProjectPath = Path.GetFullPath(projectPath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return fullProjectPath.StartsWith(fullOutputDirectory, comparison) &&
               !string.Equals(
                   fullProjectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                   fullOutputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                   comparison);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
