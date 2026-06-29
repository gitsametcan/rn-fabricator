using Fabricator.Core;
using Fabricator.Core.Processes;
using Fabricator.Core.Templates;
using System.Text.Json;

namespace Fabricator.Core.Projects;

public sealed class CreateProjectService : ICreateProjectService
{
    public const string DefaultStarterId = "minimal-splash";

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly string[] ReactNativeCliArguments = ["@react-native-community/cli@latest", "init"];
    private static readonly IReadOnlyList<StarterFile> MinimalSplashStarterFiles =
    [
        new("App.tsx", """
            import React, { useEffect, useState } from 'react';
            import { MainScreen, SplashScreen } from './src/screens';

            export default function App() {
              const [isReady, setIsReady] = useState(false);

              useEffect(() => {
                const timer = setTimeout(() => setIsReady(true), 900);

                return () => clearTimeout(timer);
              }, []);

              return isReady ? <MainScreen /> : <SplashScreen />;
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
        new("src/screens/MainScreen.tsx", """
            import React from 'react';
            import { SafeAreaView, StatusBar, StyleSheet, Text, View } from 'react-native';

            export function MainScreen() {
              return (
                <SafeAreaView style={styles.safeArea}>
                  <StatusBar barStyle="dark-content" />
                  <View style={styles.container}>
                    <Text style={styles.eyebrow}>Main</Text>
                    <Text style={styles.title}>Build your app from here.</Text>
                    <Text style={styles.subtitle}>
                      Add screens, services, utils, and integrations with Fabricator templates.
                    </Text>
                  </View>
                </SafeAreaView>
              );
            }

            const styles = StyleSheet.create({
              safeArea: {
                flex: 1,
                backgroundColor: '#FFFFFF',
              },
              container: {
                flex: 1,
                alignItems: 'center',
                justifyContent: 'center',
                padding: 24,
              },
              eyebrow: {
                color: '#2563EB',
                fontSize: 14,
                fontWeight: '700',
                marginBottom: 12,
                textTransform: 'uppercase',
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
                lineHeight: 24,
                marginTop: 12,
                textAlign: 'center',
              },
            });
            """),
        new("src/screens/index.ts", """
            export { MainScreen } from './MainScreen';
            export { SplashScreen } from './SplashScreen';
            """),
        new("src/app/index.ts", "export {};\n"),
        new("src/components/index.ts", "export {};\n"),
        new("src/config/index.ts", "export {};\n"),
        new("src/constants/index.ts", "export {};\n"),
        new("src/hooks/index.ts", "export {};\n"),
        new("src/navigation/AppNavigator.tsx", """
            import React from 'react';
            import { AuthNavigator } from './AuthNavigator';
            import { MainNavigator } from './MainNavigator';
            import type { AppNavigatorProps } from './types';

            export function AppNavigator({ isAuthenticated = true }: AppNavigatorProps) {
              return isAuthenticated ? <MainNavigator /> : <AuthNavigator />;
            }
            """),
        new("src/navigation/AuthNavigator.tsx", """
            import React from 'react';
            import { SafeAreaView, StyleSheet, Text, View } from 'react-native';
            import { ROUTES } from './routes';

            export function AuthNavigator() {
              return (
                <SafeAreaView style={styles.safeArea}>
                  <View style={styles.container}>
                    <Text style={styles.eyebrow}>{ROUTES.auth.signIn}</Text>
                    <Text style={styles.title}>Auth flow</Text>
                    <Text style={styles.subtitle}>Add login, register, and password recovery screens here.</Text>
                  </View>
                </SafeAreaView>
              );
            }

            const styles = StyleSheet.create({
              safeArea: {
                flex: 1,
                backgroundColor: '#FFFFFF',
              },
              container: {
                flex: 1,
                justifyContent: 'center',
                padding: 24,
              },
              eyebrow: {
                color: '#2563EB',
                fontSize: 14,
                fontWeight: '700',
                marginBottom: 12,
                textTransform: 'uppercase',
              },
              title: {
                color: '#111827',
                fontSize: 28,
                fontWeight: '700',
              },
              subtitle: {
                color: '#4B5563',
                fontSize: 16,
                lineHeight: 24,
                marginTop: 12,
              },
            });
            """),
        new("src/navigation/MainNavigator.tsx", """
            import React from 'react';
            import { MainScreen } from '../screens';

            export function MainNavigator() {
              return <MainScreen />;
            }
            """),
        new("src/navigation/TabNavigator.tsx", """
            import React from 'react';
            import { SafeAreaView, StyleSheet, Text, View } from 'react-native';
            import { TAB_ROUTES } from './routes';

            export function TabNavigator() {
              return (
                <SafeAreaView style={styles.safeArea}>
                  <View style={styles.container}>
                    <Text style={styles.title}>Tabs</Text>
                    {TAB_ROUTES.map(route => (
                      <View key={route.name} style={styles.item}>
                        <Text style={styles.itemTitle}>{route.title}</Text>
                        <Text style={styles.itemName}>{route.name}</Text>
                      </View>
                    ))}
                  </View>
                </SafeAreaView>
              );
            }

            const styles = StyleSheet.create({
              safeArea: {
                flex: 1,
                backgroundColor: '#F8FAFC',
              },
              container: {
                flex: 1,
                gap: 12,
                padding: 24,
              },
              title: {
                color: '#111827',
                fontSize: 28,
                fontWeight: '700',
                marginBottom: 8,
              },
              item: {
                backgroundColor: '#FFFFFF',
                borderColor: '#E5E7EB',
                borderRadius: 8,
                borderWidth: 1,
                padding: 16,
              },
              itemTitle: {
                color: '#111827',
                fontSize: 16,
                fontWeight: '700',
              },
              itemName: {
                color: '#6B7280',
                fontSize: 13,
                marginTop: 4,
              },
            });
            """),
        new("src/navigation/routes.ts", """
            export const ROUTES = {
              auth: {
                signIn: 'auth.signIn',
                forgotPassword: 'auth.forgotPassword',
              },
              main: {
                home: 'main.home',
              },
              tabs: {
                home: 'tabs.home',
                settings: 'tabs.settings',
              },
            } as const;

            export const TAB_ROUTES = [
              { name: ROUTES.tabs.home, title: 'Home' },
              { name: ROUTES.tabs.settings, title: 'Settings' },
            ] as const;
            """),
        new("src/navigation/types.ts", """
            import { ROUTES } from './routes';

            export type AuthRouteName =
              | typeof ROUTES.auth.signIn
              | typeof ROUTES.auth.forgotPassword;

            export type MainRouteName = typeof ROUTES.main.home;

            export type TabRouteName =
              | typeof ROUTES.tabs.home
              | typeof ROUTES.tabs.settings;

            export type AppRouteName = AuthRouteName | MainRouteName | TabRouteName;

            export type AppNavigatorProps = {
              isAuthenticated?: boolean;
            };

            export type NavigationCommand = {
              name: AppRouteName;
              params?: Record<string, unknown>;
            };
            """),
        new("src/navigation/linking.ts", """
            import { ROUTES } from './routes';

            export const linkingConfig = {
              prefixes: ['myapp://'],
              config: {
                screens: {
                  [ROUTES.auth.signIn]: 'sign-in',
                  [ROUTES.main.home]: 'home',
                  [ROUTES.tabs.settings]: 'settings',
                },
              },
            };
            """),
        new("src/navigation/navigationRef.ts", """
            import type { AppRouteName, NavigationCommand } from './types';

            type NavigationListener = (command: NavigationCommand) => void;

            let currentRoute: AppRouteName | null = null;
            const listeners = new Set<NavigationListener>();

            export const navigationRef = {
              getCurrentRoute() {
                return currentRoute;
              },
              navigate(name: AppRouteName, params?: Record<string, unknown>) {
                currentRoute = name;
                const command: NavigationCommand = params === undefined ? { name } : { name, params };

                listeners.forEach(listener => listener(command));
              },
              subscribe(listener: NavigationListener) {
                listeners.add(listener);

                return () => listeners.delete(listener);
              },
            };
            """),
        new("src/navigation/screenOptions.ts", """
            export const defaultScreenOptions = {
              headerShown: false,
              contentStyle: {
                backgroundColor: '#FFFFFF',
              },
            } as const;

            export const modalScreenOptions = {
              presentation: 'modal',
              headerShown: true,
            } as const;
            """),
        new("src/navigation/index.ts", """
            export { AppNavigator } from './AppNavigator';
            export { AuthNavigator } from './AuthNavigator';
            export { MainNavigator } from './MainNavigator';
            export { TabNavigator } from './TabNavigator';
            export { linkingConfig } from './linking';
            export { navigationRef } from './navigationRef';
            export { ROUTES, TAB_ROUTES } from './routes';
            export { defaultScreenOptions, modalScreenOptions } from './screenOptions';
            export type {
              AppNavigatorProps,
              AppRouteName,
              AuthRouteName,
              MainRouteName,
              NavigationCommand,
              TabRouteName,
            } from './types';
            """),
        new("src/services/index.ts", "export {};\n"),
        new("src/storage/index.ts", "export {};\n"),
        new("src/theme/index.ts", "export {};\n"),
        new("src/types/index.ts", "export {};\n"),
        new("src/utils/index.ts", "export {};\n")
    ];

    private readonly IProjectFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;
    private readonly ITemplateCatalogProvider _templateCatalogProvider;
    private readonly CreateProjectValidator _validator;

    public CreateProjectService(
        CreateProjectValidator validator,
        IProcessRunner processRunner)
        : this(validator, processRunner, new SystemProjectFileSystem(), new TemplateCatalogProvider())
    {
    }

    public CreateProjectService(
        CreateProjectValidator validator,
        IProcessRunner processRunner,
        IProjectFileSystem fileSystem)
        : this(validator, processRunner, fileSystem, new TemplateCatalogProvider())
    {
    }

    public CreateProjectService(
        CreateProjectValidator validator,
        IProcessRunner processRunner,
        IProjectFileSystem fileSystem,
        ITemplateCatalogProvider templateCatalogProvider)
    {
        _validator = validator;
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _templateCatalogProvider = templateCatalogProvider;
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

        var starterResult = await ApplyStarterAsync(validation, cancellationToken);
        if (starterResult.Succeeded)
        {
            starterResult = ApplyProjectContractManifest(validation.FullProjectPath, starterResult);
        }

        if (starterResult.Succeeded)
        {
            starterResult = ApplyProjectStateFile(validation, starterResult);
        }

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

    private async Task<CreateProjectStarterResult> ApplyStarterAsync(
        CreateProjectValidationResult validation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(validation.Request.TemplateSource))
        {
            return ApplyMinimalSplashStarter(validation.FullProjectPath);
        }

        try
        {
            var package = await _templateCatalogProvider.GetTemplateAsync(
                validation.Request.TemplateSource,
                validation.Request.TemplateName,
                cancellationToken);

            if (!string.Equals(package.Manifest.Mode, "starter", StringComparison.OrdinalIgnoreCase))
            {
                return CreateProjectStarterResult.Failed(
                    package.Manifest.Id,
                    package.Manifest.Version,
                    package.Manifest.Category ?? "starter",
                    [],
                    [$"Template '{package.Manifest.Id}' is not a create starter. Use a template copy command for mode '{package.Manifest.Mode}'."]);
            }

            return ApplyStarterFiles(
                validation.FullProjectPath,
                package.Manifest.Id,
                package.Files,
                package.Manifest.Version,
                package.Manifest.Category ?? "starter");
        }
        catch (TemplatePackageException exception)
        {
            return CreateProjectStarterResult.Failed(
                validation.Request.TemplateName,
                [],
                [exception.Message]);
        }
        catch (HttpRequestException exception)
        {
            return CreateProjectStarterResult.Failed(
                validation.Request.TemplateName,
                [],
                [$"Template catalog request failed: {exception.Message}"]);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateProjectStarterResult.Failed(
                validation.Request.TemplateName,
                [],
                [$"Template catalog request timed out: {exception.Message}"]);
        }
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

        return ApplyStarterFiles(
            projectPath,
            DefaultStarterId,
            MinimalSplashStarterFiles.ToDictionary(file => file.RelativePath, file => file.Contents, StringComparer.Ordinal));
    }

    private CreateProjectStarterResult ApplyStarterFiles(
        string projectPath,
        string starterId,
        IReadOnlyDictionary<string, string> files,
        string starterVersion = "0.1.0",
        string category = "starter")
    {
        var generatedFiles = new List<string>();
        var errors = new List<string>();

        if (!_fileSystem.DirectoryExists(projectPath))
        {
            return CreateProjectStarterResult.Failed(
                starterId,
                generatedFiles,
                [$"Generated project directory was not found before applying starter: {projectPath}"]);
        }

        foreach (var file in files)
        {
            var targetPath = Path.GetFullPath(Path.Combine(projectPath, file.Key));

            if (!IsSafeGeneratedProjectPath(projectPath, targetPath))
            {
                errors.Add($"Starter file resolved outside the generated project: {file.Key}");
                continue;
            }

            try
            {
                var targetDirectory = Path.GetDirectoryName(targetPath);

                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    _fileSystem.CreateDirectory(targetDirectory);
                }

                _fileSystem.WriteAllText(targetPath, file.Value);
                generatedFiles.Add(file.Key);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                errors.Add($"Failed to write starter file {file.Key}: {exception.Message}");
            }
        }

        return errors.Count == 0
            ? CreateProjectStarterResult.Applied(starterId, starterVersion, category, generatedFiles)
            : CreateProjectStarterResult.Failed(starterId, starterVersion, category, generatedFiles, errors);
    }

    private CreateProjectStarterResult ApplyProjectContractManifest(
        string projectPath,
        CreateProjectStarterResult starterResult)
    {
        var manifest = FabricatorProjectContract.CreateManifest(ProductInfo.Version);
        var manifestContents = JsonSerializer.Serialize(manifest, ManifestJsonOptions) + System.Environment.NewLine;
        var contractResult = ApplyStarterFiles(
            projectPath,
            starterResult.StarterId,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [FabricatorProjectContract.ManifestRelativePath] = manifestContents
            });

        var generatedFiles = starterResult.GeneratedFiles
            .Concat(contractResult.GeneratedFiles)
            .ToArray();
        var errors = starterResult.Errors
            .Concat(contractResult.Errors)
            .ToArray();

        return errors.Length == 0
            ? CreateProjectStarterResult.Applied(
                starterResult.StarterId,
                starterResult.StarterVersion,
                starterResult.Category,
                generatedFiles)
            : CreateProjectStarterResult.Failed(
                starterResult.StarterId,
                starterResult.StarterVersion,
                starterResult.Category,
                generatedFiles,
                errors);
    }

    private CreateProjectStarterResult ApplyProjectStateFile(
        CreateProjectValidationResult validation,
        CreateProjectStarterResult starterResult)
    {
        var projectPath = validation.FullProjectPath;
        var statePath = Path.GetFullPath(Path.Combine(projectPath, FabricatorProjectStateContract.StateRelativePath));

        if (!IsSafeGeneratedProjectPath(projectPath, statePath))
        {
            return CreateProjectStarterResult.Failed(
                starterResult.StarterId,
                starterResult.StarterVersion,
                starterResult.Category,
                starterResult.GeneratedFiles,
                [$"Project state file resolved outside the generated project: {FabricatorProjectStateContract.StateRelativePath}"]);
        }

        if (_fileSystem.FileExists(statePath))
        {
            return starterResult;
        }

        try
        {
            var state = FabricatorProjectStateContract.CreateInitialState(
                validation.Request.ProjectName,
                ProductInfo.Version,
                starterResult.StarterId,
                starterResult.StarterVersion,
                starterResult.Category,
                validation.Request.TemplateSource,
                starterResult.GeneratedFiles);
            var stateContents = JsonSerializer.Serialize(state, ManifestJsonOptions) + System.Environment.NewLine;

            _fileSystem.WriteAllText(statePath, stateContents);

            return CreateProjectStarterResult.Applied(
                starterResult.StarterId,
                starterResult.StarterVersion,
                starterResult.Category,
                starterResult.GeneratedFiles
                    .Concat([FabricatorProjectStateContract.StateRelativePath])
                    .ToArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CreateProjectStarterResult.Failed(
                starterResult.StarterId,
                starterResult.StarterVersion,
                starterResult.Category,
                starterResult.GeneratedFiles,
                [$"Failed to write project state file {FabricatorProjectStateContract.StateRelativePath}: {exception.Message}"]);
        }
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
