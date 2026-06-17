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
rn-fabricator create <name> --template basic-auth
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

Templates are stored in the repository and shipped with the CLI package as copied local assets.

Runtime template layout:

```text
Templates/
  basic-auth/
    template.json
    ...
```

Source template layout:

```text
src/Fabricator.Core/
  TemplateAssets/
    basic-auth/
      template.json
      ...
```

`Fabricator.Core` copies `TemplateAssets/**/*` to `Templates/` in build and publish output. Runtime services resolve templates from `AppContext.BaseDirectory/Templates` by default, so local development, tests, and packaged tool usage use the same lookup strategy.

Template application must:

- Read template metadata from `template.json`.
- Avoid overwriting user files unless explicitly allowed.
- Generate example config files, not real secret files.
- Keep generated React Native code readable.

## Error Handling

Project creation must validate the target path before invoking external tools. If React Native CLI fails after creating the target project directory, rn-fabricator should remove only that generated project directory.

It must not delete the output directory itself, existing files, or paths outside the validated output directory.

Commands should return stable exit codes:

- `0`: Success.
- `1`: General failure.
- `2`: Invalid input.
- `3`: Environment requirement failure.

Error messages should explain what failed and what the user can do next.

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
