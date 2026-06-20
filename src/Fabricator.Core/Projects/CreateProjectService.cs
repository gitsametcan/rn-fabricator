using Fabricator.Core.Processes;

namespace Fabricator.Core.Projects;

public sealed class CreateProjectService : ICreateProjectService
{
    public const string DefaultStarterId = "minimal-splash";

    private static readonly string[] ReactNativeCliArguments = ["@react-native-community/cli@latest", "init"];
    private static readonly IReadOnlyList<StarterFile> MinimalSplashStarterFiles =
    [
        new("App.tsx", """
            import React from 'react';
            import { SplashScreen } from './src/screens';

            export default function App() {
              return <SplashScreen />;
            }
            """),
        new("src/screens/SplashScreen.tsx", """
            import React from 'react';
            import { SafeAreaView, StatusBar, StyleSheet, Text, View } from 'react-native';

            export function SplashScreen() {
              return (
                <SafeAreaView style={styles.safeArea}>
                  <StatusBar barStyle="dark-content" />
                  <View style={styles.container}>
                    <Text style={styles.brand}>rn-fabricator</Text>
                    <Text style={styles.title}>Your React Native app is ready.</Text>
                    <Text style={styles.subtitle}>Start from this minimal splash screen.</Text>
                  </View>
                </SafeAreaView>
              );
            }

            const styles = StyleSheet.create({
              safeArea: {
                flex: 1,
                backgroundColor: '#F7F8FA',
              },
              container: {
                flex: 1,
                alignItems: 'center',
                justifyContent: 'center',
                padding: 24,
              },
              brand: {
                color: '#2563EB',
                fontSize: 16,
                fontWeight: '700',
                marginBottom: 16,
              },
              title: {
                color: '#111827',
                fontSize: 28,
                fontWeight: '700',
                textAlign: 'center',
              },
              subtitle: {
                color: '#4B5563',
                fontSize: 16,
                marginTop: 12,
                textAlign: 'center',
              },
            });
            """),
        new("src/screens/index.ts", "export { SplashScreen } from './SplashScreen';\n"),
        new("src/app/index.ts", "export {};\n"),
        new("src/components/index.ts", "export {};\n"),
        new("src/config/index.ts", "export {};\n"),
        new("src/constants/index.ts", "export {};\n"),
        new("src/hooks/index.ts", "export {};\n"),
        new("src/services/index.ts", "export {};\n"),
        new("src/storage/index.ts", "export {};\n"),
        new("src/theme/index.ts", "export {};\n"),
        new("src/types/index.ts", "export {};\n"),
        new("src/utils/index.ts", "export {};\n")
    ];

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
            validation.FullOutputDirectory,
            validation.Request.OnStandardOutput,
            validation.Request.OnStandardError);
        validation.Request.OnCommandPrepared?.Invoke(command);

        var processResult = await _processRunner.RunAsync(command, cancellationToken);
        if (!processResult.Succeeded)
        {
            return CreateProjectResult.Completed(
                validation,
                command,
                processResult,
                RollBackPartialProject(validation));
        }

        var starterResult = ApplyMinimalSplashStarter(validation.FullProjectPath);
        var rollback = starterResult.Succeeded
            ? CreateProjectRollbackResult.NotRequired("Rollback was not required because project creation succeeded.")
            : RollBackPartialProject(validation);
        var completedProcessResult = starterResult.Succeeded
            ? processResult
            : new ProcessRunResult(
                ExitCodes.GeneralFailure,
                processResult.StandardOutput,
                string.Join(System.Environment.NewLine, starterResult.Errors));

        return CreateProjectResult.Completed(
            validation,
            command,
            completedProcessResult,
            rollback,
            starterResult);
    }

    private CreateProjectStarterResult ApplyMinimalSplashStarter(string projectPath)
    {
        var generatedFiles = new List<string>();
        var errors = new List<string>();

        if (!_fileSystem.DirectoryExists(projectPath))
        {
            return CreateProjectStarterResult.Failed(
                DefaultStarterId,
                generatedFiles,
                [$"Generated project directory was not found before applying starter: {projectPath}"]);
        }

        foreach (var file in MinimalSplashStarterFiles)
        {
            var targetPath = Path.GetFullPath(Path.Combine(projectPath, file.RelativePath));

            if (!IsSafeGeneratedProjectPath(projectPath, targetPath))
            {
                errors.Add($"Starter file resolved outside the generated project: {file.RelativePath}");
                continue;
            }

            try
            {
                var targetDirectory = Path.GetDirectoryName(targetPath);

                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    _fileSystem.CreateDirectory(targetDirectory);
                }

                _fileSystem.WriteAllText(targetPath, file.Contents);
                generatedFiles.Add(file.RelativePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                errors.Add($"Failed to write starter file {file.RelativePath}: {exception.Message}");
            }
        }

        return errors.Count == 0
            ? CreateProjectStarterResult.Applied(DefaultStarterId, generatedFiles)
            : CreateProjectStarterResult.Failed(DefaultStarterId, generatedFiles, errors);
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

    private sealed record StarterFile(string RelativePath, string Contents);
}
