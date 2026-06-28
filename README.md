# rn-fabricator

rn-fabricator is a .NET 8 command-line tool that speeds up React Native CLI project setup.

It helps developers create new React Native projects, verify their local development environment, apply starter templates, generate example configuration files, and run release preparation checks before shipping mobile apps.

## Project Status

Status: Early development

The project has an initial .NET solution, CLI shell, tests, CI workflow, a working `doctor` command, and a React Native CLI-backed `create` command.

## MVP Scope

- `doctor`: Check React Native CLI development requirements such as Node.js, npm, Git, Watchman, Xcode, CocoaPods, Java, and Android SDK.
- `create`: Generate a new React Native CLI project, validate output paths, clean up partial failures, and print next steps.
- `basic-auth` template: Add Splash, Loading, Login, and Home screens with a simple authentication flow plus `.env.example` and `credentials.example.json`.

## Planned Technology

- .NET 8
- System.CommandLine
- xUnit
- GitHub Actions

Current solution layout:

```text
src/
  Fabricator.Core/
  Fabricator.Cli/
tests/
  Fabricator.Tests/
```

## Local Development

See [Local Development](docs/development.md) for setup, build, test, run, branch workflow, and local secrets guidance.

CI-equivalent local checks:

```bash
dotnet restore rn-fabricator.sln
dotnet build rn-fabricator.sln --no-restore --configuration Release
dotnet test rn-fabricator.sln --no-build --configuration Release
```

## Installation

rn-fabricator targets .NET 8 and is currently prepared as a pre-release .NET tool.

Install from NuGet after the first package is published:

```bash
dotnet tool install --global rn-fabricator
```

Update an existing global installation:

```bash
dotnet tool update --global rn-fabricator
```

Install from a local package while developing the repository:

```bash
dotnet pack src/Fabricator.Cli/Fabricator.Cli.csproj --configuration Release --output artifacts/packages -p:VersionPrefix=1.0.0 -p:VersionSuffix=beta.3
dotnet tool install rn-fabricator --tool-path ./.tools --add-source artifacts/packages --version 1.0.0-beta.4
./.tools/rn-fabricator --help
```

## Usage

Check the local React Native CLI development environment:

```bash
rn-fabricator doctor
```

Print a guided setup plan without running install commands. This command is read-only and is safe to run before changing your machine:

```bash
rn-fabricator setup plan
```

Apply safe setup commands with per-step confirmation. The CLI prints each executable command and waits for `y` or `yes`; empty input or any other response skips that command:

```bash
rn-fabricator setup apply
```

Preview setup execution without prompts or installs:

```bash
rn-fabricator setup apply --dry-run
```

Run only safe allowlisted setup commands without prompts. Elevated commands such as `sudo`, GUI installs, and environment variable edits remain manual:

```bash
rn-fabricator setup apply --yes
```

Use a specific toolchain profile or React Native version for setup recommendations:

```bash
rn-fabricator setup plan --profile react-native-stable
rn-fabricator setup plan --react-native 0.76.x
rn-fabricator setup apply --react-native 0.76.x
```

Create a React Native CLI project with the default `minimal-splash` starter:

```bash
rn-fabricator create MyApp
```

Create a project in a specific output directory:

```bash
rn-fabricator create MyApp --output ./sandbox
```

Create with an explicit Fabricator template catalog source:

```bash
rn-fabricator create MyApp --template-source ./templates/catalog.fabricator.json
```

For beta releases, use the matching tagged catalog when you want reproducible examples:

```bash
rn-fabricator create MyApp \
  --template-source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/v1.0.0-beta.4/templates/catalog.fabricator.json
```

For local template authoring, keep the project and the editable template workspace side by side:

```text
fabricator-playground/
  MyApp/
  local-templates/
    catalog.fabricator.json
```

Use `local-templates/catalog.fabricator.json` as the local catalog source while capturing, updating, validating, and applying templates.

List templates from a Fabricator template catalog:

```bash
rn-fabricator templates list
rn-fabricator templates list --source ./templates/catalog.fabricator.json
rn-fabricator templates list --category auth --source ./templates/catalog.fabricator.json
rn-fabricator templates list --category component --source ./templates/catalog.fabricator.json
rn-fabricator templates list --category screen --source ./templates/catalog.fabricator.json
rn-fabricator templates list --category navigation --source ./templates/catalog.fabricator.json
```

Inspect a template before copying or applying it:

```bash
rn-fabricator templates info component/primary-button
rn-fabricator templates info basic-auth --source ./templates/catalog.fabricator.json
rn-fabricator templates info component/primary-button --source ./templates/catalog.fabricator.json
rn-fabricator templates info component/empty-state --source ./templates/catalog.fabricator.json
rn-fabricator templates info screen/settings-screen --source ./templates/catalog.fabricator.json
rn-fabricator templates info navigation/app-navigator --source ./templates/catalog.fabricator.json
```

Validate a template catalog before applying or publishing templates:

```bash
rn-fabricator templates validate
rn-fabricator templates validate --source ./templates/catalog.fabricator.json
```

Copy a template from a Fabricator template catalog into an existing project:

```bash
rn-fabricator templates copy basic-auth --source ./templates/catalog.fabricator.json --output ./MyApp
```

Apply a template to a compatible Fabricator project:

```bash
rn-fabricator templates apply basic-auth --source ./templates/catalog.fabricator.json --output ./MyApp
rn-fabricator templates apply basic-auth --source ./templates/catalog.fabricator.json --output ./MyApp --dry-run
rn-fabricator templates apply screen/settings-screen --source ./templates/catalog.fabricator.json --output ./MyApp --dry-run
rn-fabricator templates apply component/empty-state --source ./templates/catalog.fabricator.json --output ./MyApp --dry-run
rn-fabricator templates apply navigation/app-navigator --source ./templates/catalog.fabricator.json --output ./MyApp --dry-run
```

`templates apply` validates `.fabricator/project.json`, writes files non-destructively, adds supported barrel exports such as `src/screens/index.ts`, records the operation in root `fabricator.json`, and prints manual integration notes for anything it cannot safely automate.

Use `--dry-run` on template lifecycle commands to preview planned writes without changing project files, local catalogs, template folders, exports, or `fabricator.json`.

Show template history for a compatible Fabricator project:

```bash
rn-fabricator templates status --project ./MyApp
```

`templates status` reads root `fabricator.json`, summarizes applied templates, and compares local catalog versions when the recorded template source is available on disk.

Template lifecycle commands use root `fabricator.json` as the user-facing project state file. See [Fabricator State Contract](docs/fabricator-state-contract.md) for the state shape.

Template commands resolve catalog sources in this order: explicit `--source`, `RN_FABRICATOR_TEMPLATE_SOURCE`, catalog sources from root `fabricator.json`, then conventional local `./templates/catalog.fabricator.json`.

`fabricator.json` is created by `rn-fabricator create`. Template apply records successful operations there, and status reads it later. If the file is missing or invalid, template commands fail with a repair-oriented error instead of guessing.

Capture a reusable template from a compatible Fabricator project folder:

```bash
rn-fabricator templates capture profile-screen --category screen --from ./MyApp --output ./templates
```

Capture only one screen, component, service, or utility by passing project-relative `--include` paths:

```bash
rn-fabricator templates capture profile-screen --category screen --from ./MyApp --output ./templates --include src/screens/ProfileScreen.tsx
```

Capture navigation files as a local navigation template:

```bash
rn-fabricator templates add navigation/app-navigator \
  --category navigation \
  --from ./MyApp \
  --source ./templates/catalog.fabricator.json \
  --include src/navigation/AppNavigator.tsx \
  --include src/navigation/AuthNavigator.tsx \
  --include src/navigation/MainNavigator.tsx \
  --include src/navigation/TabNavigator.tsx \
  --include src/navigation/routes.ts \
  --include src/navigation/types.ts \
  --include src/navigation/linking.ts \
  --include src/navigation/navigationRef.ts \
  --include src/navigation/screenOptions.ts \
  --include src/navigation/index.ts
```

Capture and register a reusable template in a local catalog:

```bash
rn-fabricator templates add profile-screen --category screen --from ./MyApp --source ./templates/catalog.fabricator.json
rn-fabricator templates add profile-screen --category screen --from ./MyApp --source ./templates/catalog.fabricator.json --include src/screens/ProfileScreen.tsx
rn-fabricator templates add profile-screen --category screen --from ./MyApp --source ./templates/catalog.fabricator.json --dry-run
```

Refresh an existing local template from a compatible project:

```bash
rn-fabricator templates update profile-screen --from ./MyApp --source ./templates/catalog.fabricator.json
rn-fabricator templates update profile-screen --from ./MyApp --source ./templates/catalog.fabricator.json --include src/screens/ProfileScreen.tsx
rn-fabricator templates update profile-screen --from ./MyApp --source ./templates/catalog.fabricator.json --dry-run
```

Remove a template from a local catalog without deleting files:

```bash
rn-fabricator templates remove profile-screen --source ./templates/catalog.fabricator.json
rn-fabricator templates remove profile-screen --source ./templates/catalog.fabricator.json --dry-run
```

Template files are kept by default. Use `--delete-files` only when you explicitly want to remove the local template folder too.

Fabricator can add files and safe barrel exports automatically. It intentionally leaves arbitrary imports, navigation registration, menu wiring, dependency installation, and secret/env decisions as manual integration notes.

See [Reusable Template Workflow](docs/reusable-template-workflow.md) for the full create, list, info, apply, capture, add, update, and remove flow.

Useful help commands:

```bash
rn-fabricator --help
rn-fabricator doctor --help
rn-fabricator setup plan --help
rn-fabricator setup apply --help
rn-fabricator create --help
rn-fabricator templates list --help
rn-fabricator templates info --help
rn-fabricator templates validate --help
rn-fabricator templates apply --help
rn-fabricator templates capture --help
rn-fabricator templates add --help
rn-fabricator templates update --help
rn-fabricator templates remove --help
rn-fabricator templates copy --help
```

## Troubleshooting

- Confirm .NET 8 is installed: [Download .NET](https://dotnet.microsoft.com/download/dotnet/8.0)
- Review .NET tool install behavior: [dotnet tool install](https://learn.microsoft.com/dotnet/core/tools/dotnet-tool-install)
- Prepare React Native CLI dependencies: [Set up your environment](https://reactnative.dev/docs/environment-setup)
- Check project-specific development steps: [Local Development](docs/development.md)
- Review release readiness checks: [Release Checklist](docs/release-checklist.md)
- If `setup plan` says the package manager is not detected, install the platform package manager first, for example Homebrew on macOS, or follow the printed manual steps.
- If you decline a `setup apply` prompt, rn-fabricator skips that command and continues with the rest of the plan.
- `sudo` commands, Xcode, Android Studio, shell profile edits, and Android SDK environment variables are intentionally not automated in the first guided setup implementation.
- Use [Dogfooding Workflow](docs/dogfooding.md) to test rn-fabricator from a repo-external playground directory.

## Documentation

- [Product Brief](docs/product-brief.md)
- [Requirements](docs/requirements.md)
- [Architecture](docs/architecture.md)
- [Local Development](docs/development.md)
- [Dogfooding Workflow](docs/dogfooding.md)
- [Roadmap](ROADMAP.md)
- [Architecture Decisions](docs/decisions.md)
- [Guided Setup Design](docs/guided-setup.md)
- [Setup Apply Safety Policy](docs/setup-apply-safety.md)
- [Template Catalog](docs/template-catalog.md)
- [Fabricator Project Contract](docs/fabricator-project-contract.md)
- [Fabricator State Contract](docs/fabricator-state-contract.md)
- [Reusable Template Workflow](docs/reusable-template-workflow.md)
- [Release Checklist](docs/release-checklist.md)
- [Release Workflow](docs/release-workflow.md)
- [v1.0.0-beta.4 Release Notes](docs/releases/v1.0.0-beta.4.md)
- [v1.0.0-beta.3 Release Notes](docs/releases/v1.0.0-beta.3.md)
- [v1.0.0-beta.2 Release Notes](docs/releases/v1.0.0-beta.2.md)
- [v1.0.0-beta.1 Release Notes](docs/releases/v1.0.0-beta.1.md)
- [v0.9.0 Release Notes](docs/releases/v0.9.0.md)
- [v0.8.0 Release Notes](docs/releases/v0.8.0.md)
- [v0.7.1 Release Notes](docs/releases/v0.7.1.md)
- [v0.7.0-alpha.1 Release Notes](docs/releases/v0.7.0-alpha.1.md)
- [v0.6.0-alpha.1 Release Notes](docs/releases/v0.6.0-alpha.1.md)
- [v0.5.0-alpha.1 Release Notes](docs/releases/v0.5.0-alpha.1.md)

## Contributing

Contributions are welcome after the initial public repository setup is complete. See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

Please report security concerns using the process in [SECURITY.md](SECURITY.md).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
