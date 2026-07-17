# UI Expectations

This document captures the product expectations for the next rn-fabricator desktop UI milestone after `v1.1.0 - UI Foundation`.

`v1.1.0 - UI Foundation` is complete on `develop` and will not be released as a standalone version. The next UI planning target is `v1.2.0 - Workspace Discovery UI`.

## Product Direction

The desktop UI should become a local workspace control center for mobile projects and reusable Fabricator templates.

The first workflow should start from a user-selected workspace directory. That directory is expected to contain React Native mobile project folders and a local Fabricator templates folder.

On first launch, the desktop app should ask the user to select a workspace directory. After a workspace is selected, the app should remember the last selected workspace locally and reopen with that workspace by default. The user should still be able to change the workspace later.

Example:

```text
mobile-workspace/
  CustomerApp/
  AdminApp/
  SalesApp/
  templates/
    catalog.fabricator.json
```

The UI should discover this workspace, show projects on the home screen, and provide a templates entry point.

## Non-Goals For The Next Milestone

- Do not build a visual React Native app builder.
- Do not apply, update, remove, or capture templates from the UI yet.
- Do not create new React Native projects from the UI yet.
- Do not edit App Store, Play Store, or project metadata yet.
- Do not mutate local project agent memory yet.

The next milestone should be read-only discovery and display. Mutating workflows should come after the workspace model is stable.

## Home Screen

The home screen should show the selected workspace directory and a list of discovered mobile projects.

Expected home content:

- Selected workspace path.
- Project list with clickable project rows or buttons.
- Templates entry point.
- Basic project status for each discovered folder.

Project list items should prefer dense, scannable rows over large marketing-style cards. A workspace can contain many projects, so the layout should remain usable with long lists.

Suggested project row fields:

- Project display name.
- Project folder name.
- Fabricator compatibility status.
- Applied template count.
- Agent memory status.
- Last modified date.
- Short status label such as `Ready`, `Missing state`, or `Needs review`.

## Project Discovery

The UI should scan direct children of the selected workspace directory and classify them.

Initial project signals:

- `.fabricator/project.json`
- root `fabricator.json`
- `package.json`
- `app.json`
- `ios/` and `android/` folders

The first implementation should not require every project to be Fabricator-created. It should distinguish between:

- Fabricator-compatible React Native projects.
- React Native-looking projects missing Fabricator state.
- Folders that are not mobile projects.

React Native-looking projects without Fabricator state should still appear in the main project list with a clear `Missing Fabricator state` label. This keeps the UI useful for existing mobile projects and leaves room for a future adopt or migrate workflow.

Non-project folders should not dominate the home screen. The UI can hide them or place them in a secondary diagnostics area later.

## Templates Discovery

The UI should look for a local catalog at:

```text
<workspace>/templates/catalog.fabricator.json
```

If found, the Templates entry should open a grouped read-only catalog view.

If missing, the UI should show a clear empty state that explains the expected path. It should not create files in the first milestone.

## Templates Screen

The first Templates screen only needs to list templates under their categories.

Expected fields:

- Template category.
- Display name.
- Template id.
- Description.
- Version.
- Tags.

Template actions such as apply, copy, validate, add, update, remove, and capture are intentionally out of scope for the first workspace discovery milestone.

## App Detail Screen

Clicking a project from the home screen should open a read-only app detail page.

Expected sections:

1. Overview
2. Store Metadata
3. Applied Templates
4. Project Statistics
5. Agent Memory

### Overview

Show the core identity and compatibility state of the app.

Suggested fields:

- App display name.
- Project folder path.
- React Native package name when available.
- Fabricator project contract status.
- Root `fabricator.json` status.
- Last known template operation when available.

### Store Metadata

Show App Store and Play Store naming metadata when it can be discovered safely.

Suggested fields:

- App Store display name.
- Play Store display name.
- iOS bundle identifier.
- Android application id.
- App version.
- Build number or version code.

Important caveat:

Store display names are not guaranteed to exist in one standard local file. The UI should treat this section as best-effort discovery at first and show missing metadata clearly.

Future direction:

If this metadata becomes important to the product workflow, add an explicit Fabricator metadata contract instead of relying only on platform-specific file parsing.

### Applied Templates

Show templates recorded in root `fabricator.json`.

Suggested fields:

- Template id.
- Template display name when resolvable from the current catalog.
- Applied version.
- Source catalog.
- Applied date or operation timestamp when available.
- Status such as `Current`, `Unknown`, or `Source missing`.

The first milestone should not try to perform update checks unless the required source is local and cheap to read.

### Project Statistics

The statistics section should provide a quick read on project shape without becoming a full analytics product.

Initial statistics:

- Total source files under `src/`.
- Screen file count.
- Component file count.
- Service file count.
- Utility file count.
- Applied template count.
- Last modified date.

Potential later statistics:

- Git branch.
- Git dirty state.
- Test count.
- Dependency count.
- Native platform configuration summary.

### Agent Memory

The app detail page should detect whether the project has local agent memory.

Initial signals:

- `AGENTS.md`
- `.agents/`
- `.agents/handoff.md`
- `.agents/current-focus.md`
- `.agents/project-map.md`

Suggested fields:

- Agent memory status.
- Handoff file last modified date.
- Current focus summary when available.
- Open question count when easy to detect.

The first milestone should only detect and display this state. Creating or editing project agent memory can be a later workflow.

## First Workspace UI Milestone

Suggested milestone name:

```text
v1.2.0 - Workspace Discovery UI
```

Milestone purpose:

Build the first real desktop UI workflow around a selected local workspace, read-only project discovery, app detail inspection, and grouped template catalog browsing.

Milestone boundaries:

- Read-only.
- Local filesystem only.
- First launch asks for a workspace directory.
- Last selected workspace is remembered locally.
- React Native-looking projects missing Fabricator state appear in the main list with `Missing Fabricator state`.
- App Store and Play Store metadata use best-effort parsing with explicit missing states.
- No project creation.
- No template mutation.
- No setup apply.
- No agent memory mutation.
- No standalone `v1.1.0` release is required before this milestone.

## Proposed Issues

### Define workspace UI expectations

Acceptance criteria:

- `docs/ui-expectations.md` documents workspace root expectations.
- Home, app detail, and templates scope are documented.
- Read-only milestone boundaries are explicit.

### Add workspace root discovery service

Acceptance criteria:

- Given a workspace path, the service discovers project candidates.
- The service detects `<workspace>/templates/catalog.fabricator.json`.
- The service classifies Fabricator-compatible projects separately from React Native-looking folders missing Fabricator state.
- React Native-looking folders missing Fabricator state remain visible as project candidates.
- The service is covered by tests using a fake filesystem workspace.

### Add desktop home workspace screen

Acceptance criteria:

- First launch can start from an empty workspace selection state.
- The desktop app can remember and reload the last selected workspace path locally.
- The user can change the selected workspace.
- The desktop app can show a selected workspace path.
- Discovered projects are listed as clickable rows or buttons.
- Projects missing Fabricator state are shown with a `Missing Fabricator state` label.
- A Templates entry point is visible when a catalog is found.
- Empty or invalid workspace states are readable.

### Add read-only app detail overview

Acceptance criteria:

- Clicking a discovered project opens an app detail view.
- The view shows app identity, project path, Fabricator status, applied template count, and basic project statistics.
- App Store and Play Store metadata are discovered best-effort from local project files.
- Missing metadata is shown explicitly instead of hidden.

### Add app agent memory detection

Acceptance criteria:

- The app detail view detects root `AGENTS.md` and `.agents/`.
- The view shows whether `handoff.md` and `current-focus.md` are present.
- The view displays safe summaries without mutating local agent files.

### Add grouped template catalog view

Acceptance criteria:

- The Templates screen reads the local catalog from the selected workspace.
- Templates are grouped by category.
- Each template row shows display name, id, description, version, and tags.
- Missing catalog state is handled clearly.

### Add workspace UI smoke tests

Acceptance criteria:

- Avalonia headless tests cover home screen rendering with fake workspace data.
- App detail screen rendering is smoke tested.
- Template grouped view rendering is smoke tested.
- Tests do not invoke real external tools.

## Resolved Decisions

- Workspace selection should support both first-launch selection and local recall of the last selected workspace.
- Non-Fabricator React Native projects should appear in the main project list with a `Missing Fabricator state` label.
- App Store and Play Store metadata should use best-effort parsing in the first workspace milestone, with explicit missing states.
- `v1.1.0 - UI Foundation` will not be released as a standalone version. Continue directly with `v1.2.0 - Workspace Discovery UI`.

## Remaining Planning Questions

- Which local file should store the remembered workspace path?
- Should workspace path storage live in `Fabricator.Desktop` only, or should a small core settings abstraction exist?
- Which platform files are in scope for first-pass App Store and Play Store metadata parsing?
