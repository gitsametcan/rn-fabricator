# Changelog

All notable changes to this project will be documented in this file.

This project follows the spirit of Keep a Changelog and will use semantic versioning after the first package release.

## Unreleased

No unreleased changes.

## 1.0.0-beta.2 - 2026-06-27

### Added

- Repeatable `--include <path>` support for `templates capture`, `templates add`, and `templates update`.
- Selected-file template capture for saving one screen, component, service, utility, or small feature slice without capturing a whole Fabricator folder.
- Target folder derivation for selected files using `.fabricator/project.json`, so selected screens, components, and services apply back to the expected project paths.

### Changed

- Reusable template workflow documentation now separates full-folder templates from selected-file templates.
- Dogfooding documentation now includes a single-screen template capture, add, and update workflow.

## 1.0.0-beta.1 - 2026-06-26

### Added

- Root `fabricator.json` project state generation for Fabricator-created projects.
- Template source resolution from explicit `--source`, `RN_FABRICATOR_TEMPLATE_SOURCE`, root `fabricator.json`, and conventional local catalogs.
- `templates status` for inspecting applied template history and local catalog version status.
- Local template lifecycle commands for reusable catalogs:
  - `templates add`
  - `templates update`
  - `templates remove`
- `--dry-run` support for `templates apply`, `templates add`, `templates update`, and `templates remove`.
- Beta readiness coverage for create state generation, local source resolution, validation diagnostics, apply state tracking, duplicate apply behavior, lifecycle dry-runs, and invalid state handling.

### Changed

- Template command output now includes more consistent `Summary:` and `Next:` lines for portfolio-quality logs and clearer failure recovery.
- `templates apply` records successful operations in root `fabricator.json` and reports state tracking in command output.
- Local template lifecycle documentation now covers project-to-template-to-catalog workflows, repo-external dogfooding, beta catalog examples, and manual integration limits.

### Fixed

- `templates validate` now accepts Fabricator project folder categories such as `screens`, `components`, `services`, and `utils` for templates captured with local lifecycle commands.

## 0.9.0 - 2026-06-24

### Added

- Fabricator project contract manifest generation for projects created with the minimal starter.
- `templates info` command for inspecting template files, dependencies, exports, and integration hints.
- `templates list --category` filtering for larger template catalogs.
- `templates apply` command for applying catalog templates to Fabricator-compatible projects.
- Safe, idempotent barrel export updates for supported integration points.
- `templates capture` command for capturing reusable templates from Fabricator project folders.
- Reusable catalog examples for screen, service, util, layout, and component categories.
- End-to-end reusable template workflow documentation for create, list, info, apply, and capture.

### Changed

- Template schema v2 now includes category, tags, dependencies, exports, integration hints, `targetPath`, and `targetFolder` metadata.
- `templates copy` remains a low-level escape hatch, while `templates apply` validates the Fabricator project contract before mutating files.
- Dogfooding documentation now covers reusable template list, info, apply, and capture workflows.

## 0.8.0 - 2026-06-21

### Added

- Default `minimal-splash` starter application flow for `create`.
- External Fabricator template catalog support for `create --template-source`.
- `templates list` command for inspecting a Fabricator template catalog.
- `templates copy` command for applying optional catalog templates into an existing project.
- Repository-level `templates/catalog.fabricator.json` with `minimal-splash` and `basic-auth` entries.
- Template catalog documentation and dogfooding workflow for create/list/copy validation.

### Changed

- `create` now treats the generated project starter separately from optional templates.
- Optional templates can be copied after project creation instead of being coupled to `create`.

## 0.7.1 - 2026-06-20

### Changed

- Prefer `brew install cocoapods` for macOS CocoaPods setup when Homebrew is available.
- Clarify Android SDK Manager and shell profile setup guidance on macOS.

### Fixed

- Stream `create` command output while React Native CLI runs so long downloads and init steps no longer look stuck.
- Keep captured stdout/stderr for failure diagnostics while avoiding duplicate process output in the CLI.

## 0.7.0-alpha.1 - 2026-06-19

### Added

- Core toolchain profile model for version-aware React Native setup recommendations.
- Local toolchain profile data source for deterministic default and explicit profile selection.
- `setup plan` profile selection with `--profile` and `--react-native` options.
- Setup apply safety policy for future user-confirmed dependency installation.
- `setup apply` command with per-step confirmation for safe allowlisted setup commands.
- `setup apply --dry-run` and `setup apply --yes` modes with conservative safety boundaries.
- Setup execution result model and renderer for stable apply summaries.
- Setup apply workflow and dogfooding checklist documentation.

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
