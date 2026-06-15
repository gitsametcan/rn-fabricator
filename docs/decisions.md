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

## ADR-005: Start With Local Templates

Status: Proposed

Decision:
The MVP should ship templates with the CLI instead of downloading remote templates.

Reason:
Local templates are easier to test, version, and distribute. Remote template management can be added later if needed.
