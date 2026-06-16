# rn-fabricator

rn-fabricator is a .NET 8 command-line tool that speeds up React Native CLI project setup.

It helps developers create new React Native projects, verify their local development environment, apply starter templates, generate example configuration files, and run release preparation checks before shipping mobile apps.

## Project Status

Status: Planning

The first milestone is documentation and repository setup. The .NET solution and CLI implementation will be added after the initial project decisions are captured.

## MVP Scope

- `doctor`: Check React Native CLI development requirements such as Node.js, npm, Git, Watchman, Xcode, CocoaPods, Java, and Android SDK.
- `create`: Generate a new React Native CLI project and apply a standard project structure.
- `basic-auth` template: Add Splash, Loading, Login, and Home screens with a simple authentication flow plus `.env.example` and `credentials.example.json`.

## Planned Technology

- .NET 8
- System.CommandLine
- xUnit
- GitHub Actions

Planned solution layout:

```text
src/
  Fabricator.Core/
  Fabricator.Cli/
tests/
  Fabricator.Tests/
```

## Local Development

Restore, build, and test:

```bash
dotnet restore rn-fabricator.sln
dotnet build rn-fabricator.sln --configuration Release
dotnet test rn-fabricator.sln --configuration Release --no-build
```

Run the CLI locally:

```bash
dotnet run --project src/Fabricator.Cli/Fabricator.Cli.csproj -- --help
dotnet run --project src/Fabricator.Cli/Fabricator.Cli.csproj -- doctor
dotnet run --project src/Fabricator.Cli/Fabricator.Cli.csproj -- create MyApp --template basic-auth
```

## Documentation

- [Product Brief](docs/product-brief.md)
- [Requirements](docs/requirements.md)
- [Architecture](docs/architecture.md)
- [Roadmap](ROADMAP.md)
- [Architecture Decisions](docs/decisions.md)
- [Release Checklist](docs/release-checklist.md)

## Contributing

Contributions are welcome after the initial public repository setup is complete. See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

Please report security concerns using the process in [SECURITY.md](SECURITY.md).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
