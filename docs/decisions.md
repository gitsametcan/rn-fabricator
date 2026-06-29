# Architecture Decisions

This file records notable technical decisions. New decisions should be appended with a stable ADR number.

## ADR-001: Use .NET 8

Status: Accepted

Decision:
rn-fabricator will be built with .NET 8.

Reason:
.NET 8 is a current long-term support platform, has strong CLI tooling, supports cross-platform development, and is suitable for building a professional command-line application.

## ADR-002: Split Core Logic From CLI

Status: Accepted

Decision:
The solution will use separate `Fabricator.Core` and `Fabricator.Cli` projects.

Reason:
Keeping CLI parsing and console rendering separate from core logic makes the project easier to test and extend.

## ADR-003: Use xUnit For Tests

Status: Accepted

Decision:
Automated tests will use xUnit.

Reason:
xUnit is common in .NET projects, works well with GitHub Actions, and is a good fit for unit and command handler tests.

## ADR-004: Use System.CommandLine If Practical

Status: Proposed

Decision:
The CLI should use System.CommandLine unless implementation constraints make another library more practical.

Reason:
System.CommandLine provides structured command definitions, argument parsing, help text, and command handlers without requiring a custom parser.

## ADR-005: Ship Templates As Local Package Assets

Status: Accepted

Decision:
The MVP will ship templates with the CLI instead of downloading remote templates. Template assets live under `src/Fabricator.Core/TemplateAssets/<template-id>` with a required `template.json` manifest.

Template files are copied to `Templates/<template-id>` in build and publish output through the `Fabricator.Core` project file. At runtime, core template services resolve templates from `AppContext.BaseDirectory/Templates` by default.

Reason:
Local package assets work for local development, tests, and packaged .NET tool usage without network access. They are versioned with the CLI, easy to inspect in pull requests, and can be tested through the same runtime path the packaged tool uses.

Consequences:

- Each template must include a manifest before template application code can use it.
- Tests can load template assets from build output instead of relying on repository-relative paths.
- Remote template downloads remain out of scope for MVP and can be added later behind a separate provider.

## ADR-006: Keep Doctor Read-Only And Add Guided Setup

Status: Accepted

Decision:
`doctor` remains a read-only diagnostics command. Guided setup should use a separate `setup` command family:

```text
rn-fabricator setup plan
rn-fabricator setup apply
```

Reason:
Dependency installation can mutate the user's machine, require admin access, open GUI installers, accept licenses, or edit environment variables. A read-only `doctor` command is safe to run repeatedly, while `setup` makes mutation explicit and reviewable.

Consequences:

- `setup plan` should be implemented before any command execution.
- `setup apply` must print the plan and require per-step confirmation by default.
- Dry-run support is required before real install execution.
- Manual steps such as Xcode installation and shell profile edits stay manual in the first implementation.
- `--yes` may execute only explicitly allowlisted safe commands.
