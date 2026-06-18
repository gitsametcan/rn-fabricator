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
dotnet pack src/Fabricator.Cli/Fabricator.Cli.csproj --configuration Release --output artifacts/packages
dotnet tool install rn-fabricator --tool-path ./.tools --add-source artifacts/packages --version 0.6.0-alpha.1
./.tools/rn-fabricator --help
```

## Usage

Check the local React Native CLI development environment:

```bash
rn-fabricator doctor
```

Print a guided setup plan without running install commands:

```bash
rn-fabricator setup plan
```

Apply safe setup commands with per-step confirmation:

```bash
rn-fabricator setup apply
```

Use a specific toolchain profile or React Native version for setup recommendations:

```bash
rn-fabricator setup plan --profile react-native-stable
rn-fabricator setup plan --react-native 0.76.x
rn-fabricator setup apply --react-native 0.76.x
```

Create a React Native CLI project with the default `basic-auth` template:

```bash
rn-fabricator create MyApp
```

Create a project in a specific output directory:

```bash
rn-fabricator create MyApp --output ./sandbox --template basic-auth
```

Useful help commands:

```bash
rn-fabricator --help
rn-fabricator doctor --help
rn-fabricator setup plan --help
rn-fabricator setup apply --help
rn-fabricator create --help
```

## Troubleshooting

- Confirm .NET 8 is installed: [Download .NET](https://dotnet.microsoft.com/download/dotnet/8.0)
- Review .NET tool install behavior: [dotnet tool install](https://learn.microsoft.com/dotnet/core/tools/dotnet-tool-install)
- Prepare React Native CLI dependencies: [Set up your environment](https://reactnative.dev/docs/environment-setup)
- Check project-specific development steps: [Local Development](docs/development.md)
- Review release readiness checks: [Release Checklist](docs/release-checklist.md)

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
- [Release Checklist](docs/release-checklist.md)
- [Release Workflow](docs/release-workflow.md)
- [v0.6.0-alpha.1 Release Notes](docs/releases/v0.6.0-alpha.1.md)
- [v0.5.0-alpha.1 Release Notes](docs/releases/v0.5.0-alpha.1.md)

## Contributing

Contributions are welcome after the initial public repository setup is complete. See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

Please report security concerns using the process in [SECURITY.md](SECURITY.md).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
