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

## Documentation

- [Product Brief](docs/product-brief.md)
- [Requirements](docs/requirements.md)
- [Architecture](docs/architecture.md)
- [Local Development](docs/development.md)
- [Roadmap](ROADMAP.md)
- [Architecture Decisions](docs/decisions.md)
- [Release Checklist](docs/release-checklist.md)

## Contributing

Contributions are welcome after the initial public repository setup is complete. See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

Please report security concerns using the process in [SECURITY.md](SECURITY.md).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
