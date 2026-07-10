# Local Development

This guide explains how to set up, build, test, and run rn-fabricator locally.

## Prerequisites

Required:

- .NET SDK 8.x
- Git

Useful for later milestones:

- Node.js
- npm
- Watchman
- Xcode
- CocoaPods
- Java
- Android SDK

The later tools are not required to build the current CLI foundation, but they will be checked by the future `doctor` command.

## First-Time Setup

Clone the repository and enter the project directory:

```bash
git clone git@github.com:gitsametcan/rn-fabricator.git
cd rn-fabricator
```

Restore dependencies:

```bash
dotnet restore rn-fabricator.sln
```

## Build

```bash
dotnet build rn-fabricator.sln --configuration Release
```

For CI-equivalent local checks, use:

```bash
dotnet restore rn-fabricator.sln
dotnet build rn-fabricator.sln --no-restore --configuration Release
dotnet test rn-fabricator.sln --no-build --configuration Release
```

## Test

```bash
dotnet test rn-fabricator.sln --configuration Release
```

The test project uses xUnit. The desktop UI smoke tests use Avalonia Headless, so they run from the same solution test command without opening a real window. See [tests/README.md](../tests/README.md) for the current test structure.

## Run The CLI

Show help:

```bash
dotnet run --project src/Fabricator.Cli/Fabricator.Cli.csproj -- --help
```

Show version:

```bash
dotnet run --project src/Fabricator.Cli/Fabricator.Cli.csproj -- --version
```

Run current commands:

```bash
dotnet run --project src/Fabricator.Cli/Fabricator.Cli.csproj -- doctor
dotnet run --project src/Fabricator.Cli/Fabricator.Cli.csproj -- create MyApp
```

The `create` command prints the generated project path, `cd` guidance, Metro start command, and iOS/Android run commands after successful project creation.

Guided dependency setup is planned as a separate command family. See [Guided Setup Design](guided-setup.md).

For real usage testing, install the packed tool in a repo-external playground. See [Dogfooding Workflow](dogfooding.md).

## Run The Desktop App

The desktop UI is an Avalonia app for macOS and Windows:

```bash
dotnet run --project src/Fabricator.Desktop/Fabricator.Desktop.csproj
```

Build the desktop project locally:

```bash
dotnet build src/Fabricator.Desktop/Fabricator.Desktop.csproj --configuration Release
```

Publish local desktop artifacts:

```bash
dotnet publish src/Fabricator.Desktop/Fabricator.Desktop.csproj --configuration Release --runtime osx-arm64 --self-contained false
dotnet publish src/Fabricator.Desktop/Fabricator.Desktop.csproj --configuration Release --runtime win-x64 --self-contained false
```

Use the runtime that matches the machine or release artifact being validated.

UI changes follow the same branch workflow as the CLI:

- Start from a GitHub issue.
- Branch from `develop`.
- Open the pull request back into `develop`.
- Include local build and test output in the pull request body.

See [UI Plan](ui.md) for the UI-specific command bridge, packaging, and release expectations.

## Project Layout

```text
src/
  Fabricator.Core/   reusable application logic
  Fabricator.Cli/    command-line entry point and command definitions
  Fabricator.Desktop/ Avalonia desktop app for macOS and Windows

tests/
  Fabricator.Tests/  xUnit test project
```

## Branch Workflow

The repository uses:

- `main`: release-ready branch.
- `develop`: integration branch.
- `feature/...`: focused feature branches.

Expected flow:

```text
develop -> feature/<issue-scope> -> pull request -> develop
```

Every implementation task should be connected to a GitHub issue and milestone.

## Local Secrets

Do not commit tokens or real credentials.

Local-only environment files are ignored by git:

```text
.env
.env.*
```

For maintenance scripts, create `.env.local`:

```bash
GH_TOKEN=your_fine_grained_github_token
```

The token should be scoped only to this repository and only to the permissions needed for the task.

## Troubleshooting

If restore fails, confirm the .NET SDK version:

```bash
dotnet --info
```

If tests fail after code changes, rebuild before running tests with `--no-build`:

```bash
dotnet build rn-fabricator.sln --configuration Release
dotnet test rn-fabricator.sln --configuration Release --no-build
```

If GitHub Actions fails while local checks pass, compare the workflow commands in [.github/workflows/ci.yml](../.github/workflows/ci.yml) with the CI-equivalent local checks above.
