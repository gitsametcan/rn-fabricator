# Changelog

All notable changes to this project will be documented in this file.

This project follows the spirit of Keep a Changelog and will use semantic versioning after the first package release.

## Unreleased

### Added

- Platform-specific `doctor` remediation guidance for macOS, Windows, and Linux.
- Clean `--version` output that prints the package version without commit metadata.

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
