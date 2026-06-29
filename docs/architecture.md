# Architecture

## Overview

rn-fabricator will be built as a .NET 8 CLI application with a small core library that contains reusable domain logic. The command-line layer should stay thin and delegate most behavior to testable services.

## Planned Solution Layout

```text
src/
  Fabricator.Core/
    Environment/
    ProjectCreation/
    Templates/
    ReleaseChecks/

  Fabricator.Cli/
    Commands/
    Rendering/

tests/
  Fabricator.Tests/
```

## Project Responsibilities

### Fabricator.Core

Contains reusable application logic:

- Environment dependency checks.
- Project creation orchestration.
- Template application.
- File generation.
- Release readiness checks.
- Result models and abstractions.

### Fabricator.Cli

Contains command-line concerns:

- Command definitions.
- Argument and option parsing.
- Console rendering.
- Exit code mapping.
- Dependency injection setup.

### Fabricator.Tests

Contains automated tests:

- Unit tests for environment checks.
- Unit tests for template resolution and file generation.
- Command handler tests where useful.
- Regression tests for expected output and exit codes.

## CLI Design

Planned commands:

```text
rn-fabricator doctor
rn-fabricator setup plan
rn-fabricator setup apply
rn-fabricator create <name>
rn-fabricator template list
rn-fabricator release-check
```

Only `doctor`, `create`, and `basic-auth` are required for MVP.

## External Processes

The CLI will need to call external tools such as:

- `node`
- `npm`
- `git`
- `watchman`
- `xcodebuild`
- `pod`
- `java`

External process execution should be wrapped behind an abstraction so it can be tested without invoking real tools.

## Templates

Templates are stored in repository-hosted catalogs and can be consumed from explicit local paths or raw GitHub URLs. The early package still contains legacy built-in template assets for compatibility, but the forward path is catalog-based template discovery and application.

Catalog source layout:

```text
templates/
  catalog.fabricator.json
  minimal-splash/
    fabricator-template.json
  basic-auth/
    fabricator-template.json
```

Template application must:

- Read catalog metadata from `catalog.fabricator.json`.
- Read template metadata from `fabricator-template.json`.
- Validate the target project against the Fabricator project contract before applying future smart templates.
- Avoid overwriting user files unless explicitly allowed.
- Generate example config files, not real secret files.
- Keep generated React Native code readable.

## Fabricator Project Contract

Fabricator-created React Native projects should declare compatibility with a project manifest:

```text
.fabricator/project.json
```

The manifest defines the supported schema version, project type, source root, standard folders, and safe integration points. Future template apply and capture commands should use this contract to stop before writing files when a project is missing the expected structure.

See [Fabricator Project Contract](fabricator-project-contract.md) for the manifest shape and compatibility rules.

## Error Handling

Project creation must validate the target path before invoking external tools. If React Native CLI fails after creating the target project directory, rn-fabricator should remove only that generated project directory.

It must not delete the output directory itself, existing files, or paths outside the validated output directory.

Commands should return stable exit codes:

- `0`: Success.
- `1`: General failure.
- `2`: Invalid input.
- `3`: Environment requirement failure.

Error messages should explain what failed and what the user can do next.

`doctor` should remain read-only. Guided dependency setup belongs in the separate `setup` command family so users can review a plan before any install command runs.

## Security

- Never generate real credentials.
- Never log secrets.
- Keep generated secret-related files as examples only.
- Add `.env`, credential files, build outputs, and local tool files to `.gitignore`.

## Distribution

Potential distribution options:

- .NET local tool.
- .NET global tool.
- GitHub Releases artifacts.

The MVP should optimize for local development first, then package as a .NET tool.
