# Changelog

All notable changes to this project will be documented in this file.

This project follows the spirit of Keep a Changelog and will use semantic versioning after the first package release.

## Unreleased

### Added

- Core toolchain profile model for version-aware React Native setup recommendations.
- Local toolchain profile data source for deterministic default and explicit profile selection.
- `setup plan` profile selection with `--profile` and `--react-native` options.
- Setup apply safety policy for future user-confirmed dependency installation.
- `setup apply` command with per-step confirmation for safe allowlisted setup commands.
- `setup apply --dry-run` and `setup apply --yes` modes with conservative safety boundaries.

## 0.6.0-alpha.1 - 2026-06-17

### Added

- Platform-specific `doctor` remediation guidance for macOS, Windows, and Linux.
- Clean `--version` output that prints the package version without commit metadata.
- Dogfooding workflow documentation for repo-external playground testing.
- Guided setup design that keeps `doctor` read-only and plans a separate `setup` command family.
- `setup plan` command for read-only guided dependency setup planning.

## 0.5.0-alpha.1 - 2026-06-17

### Added

- Initial project documentation.
- Open source repository support files.
- Planned MVP scope for `doctor`, `create`, and `basic-auth`.
- .NET 8 solution layout with `Fabricator.Core`, `Fabricator.Cli`, and `Fabricator.Tests`.
- System.CommandLine-based CLI shell with `doctor` and `create` commands.
- Environment dependency checks for Node.js, npm, Git, Watchman, Xcode, CocoaPods, Java, and Android SDK inputs.
- React Native CLI project creation orchestration with validation, safe file operations, rollback behavior, and next-step output.
- Packaged `basic-auth` template with Splash, Loading, Login, and Home screens, auth flow wiring, `.env.example`, and `credentials.example.json`.
- Template application tests and local template asset packaging.
- .NET tool packaging metadata and local package verification workflow.
- Installation, usage, troubleshooting, versioning, and release workflow documentation.
