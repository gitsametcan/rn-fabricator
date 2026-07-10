# UI Plan

This document defines the initial UI direction for rn-fabricator.

## Product Role

The UI is a companion surface for existing rn-fabricator workflows:

- Environment diagnostics from `doctor`.
- Guided setup planning from `setup plan`.
- Project creation from `create`.
- Template catalog inspection, validation, apply, status, capture, add, update, remove, and copy workflows.

The UI is not a visual React Native app builder. It should make existing setup and template workflows easier to inspect, run, and recover from.

## Runtime Direction

The first UI should be a cross-platform desktop app:

- UI runtime: Avalonia UI.
- Language/runtime: C# on .NET 8.
- Targets: macOS and Windows for the first desktop release.
- Scope: local-only by default, with no remote service dependency.

This keeps the UI close to the existing .NET codebase while making the product surface a real desktop app instead of a browser page pointed at a localhost service.

## Technology Decision

Avalonia is the preferred foundation for the first desktop UI.

Options considered:

| Option | Fit | Decision |
| --- | --- | --- |
| Avalonia | Cross-platform .NET desktop UI for macOS and Windows, with C# and XAML close to the current codebase. | Choose for the first UI foundation. |
| .NET MAUI | Supports Windows and macOS, but is oriented around shared mobile and desktop app development. | Do not choose for the first desktop milestone. |
| Tauri | Produces cross-platform desktop apps with a web frontend and Rust host. | Do not choose because it adds another primary runtime and frontend stack. |
| Electron | Mature cross-platform desktop framework for web UI. | Do not choose because Chromium and Node.js packaging weight does not match the current .NET tool architecture. |
| Local web companion | Simple to prototype with a browser and localhost service. | Do not choose because the intended product surface is a desktop app. |

## Command Bridge

The UI command bridge should preserve the same operational facts a CLI user relies on:

- Command name, arguments, and options.
- Working directory.
- stdout and stderr.
- Exit code.
- Start, running, completed, canceled, and failed states.
- Structured errors for invalid input and environment failures.

The bridge should prefer `Fabricator.Core` services for reusable behavior. When current CLI behavior is the product contract, the bridge should call it through a testable abstraction instead of duplicating parsing or rendering logic in the UI layer.

The desktop app should not take a dependency on console rendering as its primary integration point. Shared use cases should move toward core services or application-level adapters that both CLI and desktop surfaces can test.

## Packaging Direction

The expected future package shape is:

- The normal .NET tool package remains the primary install artifact.
- The desktop app ships as separate macOS and Windows release artifacts.
- Release verification must build the desktop app, verify platform artifacts are produced, and smoke test app startup before publishing.
- The CLI and desktop app should share versioning and release notes when shipped together.

App-store distribution, auto-update infrastructure, code signing, notarization, and full installer polish are outside the UI foundation milestone unless they are required for basic local smoke testing.

## First Milestone Boundaries

`v1.1.0 - UI Foundation` should establish:

- Technology and packaging decision.
- Avalonia desktop project skeleton.
- Command bridge contract.
- First UI smoke tests.
- UI development documentation.

It should not implement full `doctor`, `setup`, `create`, or template manager screens before the bridge and project skeleton are stable.
