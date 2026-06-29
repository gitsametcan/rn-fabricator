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

The first UI should be a local web companion:

- Frontend: React, TypeScript, and Vite.
- Host: future .NET 8 local host started from the rn-fabricator tool surface.
- Access: browser pointed at a localhost URL printed by the tool.
- Scope: local-only by default, with no remote service dependency.

This keeps the first UI aligned with the existing .NET tool distribution model while avoiding native desktop packaging during the foundation milestone.

## Command Bridge

The UI command bridge should preserve the same operational facts a CLI user relies on:

- Command name, arguments, and options.
- Working directory.
- stdout and stderr.
- Exit code.
- Start, running, completed, canceled, and failed states.
- Structured errors for invalid input and environment failures.

The bridge should prefer `Fabricator.Core` services for reusable behavior. When current CLI behavior is the product contract, the bridge should call it through a testable abstraction instead of duplicating parsing or rendering logic in the browser.

## Packaging Direction

The expected future package shape is:

- The normal .NET tool package remains the primary install artifact.
- A future `rn-fabricator ui` command serves static UI assets packaged with the tool.
- Release verification must build the UI, verify static assets are included, and smoke test the launch command before publishing.

Separate native desktop installers, Electron bundles, Tauri bundles, and app-store distribution are out of scope for the UI foundation milestone.

## First Milestone Boundaries

`v1.1.0 - UI Foundation` should establish:

- Technology and packaging decision.
- UI project skeleton.
- Command bridge contract.
- First UI smoke tests.
- UI development documentation.

It should not implement full `doctor`, `setup`, `create`, or template manager screens before the bridge and project skeleton are stable.
