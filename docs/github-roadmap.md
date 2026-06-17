# GitHub Roadmap

This roadmap defines the initial GitHub milestones and issues for rn-fabricator.

The goal is to keep the project progression visible, reviewable, and portfolio-ready from the first implementation step.

## Milestones

### v0.1.0 - .NET CLI Foundation

Purpose:
Create the initial .NET solution, project layout, CLI entry point, test setup, and reliable CI pipeline.

Issues:

- Create .NET 8 solution and project layout
- Add System.CommandLine CLI shell
- Add xUnit test project and first smoke tests
- Make GitHub Actions run restore, build, and test
- Document local development workflow

### v0.2.0 - doctor Command MVP

Purpose:
Implement the first useful command: environment diagnostics for React Native CLI development.

Issues:

- Add external process runner abstraction
- Define dependency check result model
- Implement Node.js, npm, and Git checks
- Implement Watchman, Xcode, and CocoaPods checks
- Implement Java and Android SDK checks
- Render doctor command summary and exit codes

### v0.3.0 - create Command MVP

Purpose:
Create new React Native CLI projects through rn-fabricator with predictable validation and output.

Issues:

- Add project name and output path validation
- Add React Native CLI project creation orchestration
- Add safe file operation and rollback strategy
- Print create command next steps

### v0.4.0 - basic-auth Template

Purpose:
Ship the first starter template for a common React Native application foundation.

Issues:

- Decide template packaging strategy
- Add basic-auth template screens
- Add simple auth flow wiring
- Generate environment and credential example files
- Add template application tests

### v0.5.0 - Release Packaging

Purpose:
Prepare rn-fabricator for installation and public use as a .NET CLI tool.

Issues:

- Configure .NET tool packaging
- Add installation and usage documentation
- Add versioning and release notes workflow
- Prepare v0.5.0 GitHub release

## Labels

Initial labels:

- `type:feature`
- `type:docs`
- `type:chore`
- `area:cli`
- `area:core`
- `area:templates`
- `area:ci`
- `area:release`
- `priority:p0`
- `priority:p1`

## Operating Rules

- Every implementation task should be connected to an issue.
- Every issue should belong to a milestone unless it is exploratory.
- `develop` is the integration branch.
- Feature work should happen on `feature/...` branches.
- Pull requests should target `develop`.
- `main` should stay release-ready.
