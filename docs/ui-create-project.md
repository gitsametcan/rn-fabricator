# Create Project UI

This document defines the next desktop UI milestone after `v1.2.0 - Workspace Discovery UI`.

## Product Direction

The desktop UI should let a developer create a new React Native CLI project inside the selected workspace without leaving the app.

This is the first mutating desktop UI workflow. It writes files, invokes the React Native CLI through `npx`, applies the Fabricator starter, writes project state, and refreshes the workspace after success.

## Milestone

Suggested milestone name:

```text
v1.3.0 - Create Project UI
```

Milestone purpose:

Add a guarded desktop create workflow that collects project input, previews the command and target path, requires confirmation, runs the existing Core create service, streams output, reports success or failure, and refreshes the workspace project list.

## Scope

In scope:

- Create entry point from the workspace UI.
- Create form for project name, output directory, starter template, and template source.
- Default output directory is the selected workspace path.
- Default starter is `minimal-splash`.
- If the selected workspace has `templates/catalog.fabricator.json`, use it as the default template source.
- Review step before execution.
- Explicit confirmation before running `npx`.
- Command/log output panel.
- Success/failure result summary.
- Workspace refresh after successful create.
- Tests that do not invoke real external tools.

Out of scope:

- Visual React Native app building.
- Applying optional templates after create.
- Editing generated files.
- Running package install, iOS pods, Android builds, or app launch from the UI.
- Remote template marketplace selection.
- Automatic recovery beyond the existing Core rollback behavior.

## UX Flow

Expected flow:

```text
Workspace screen
  -> Create Project
  -> Fill form
  -> Review target and command
  -> Confirm
  -> Run
  -> Show logs
  -> Show result
  -> Refresh workspace
```

## Form Fields

Required fields:

- Project name.
- Output directory.

Defaulted fields:

- Starter template: `minimal-splash`.
- Template source: selected workspace `templates/catalog.fabricator.json` when present; otherwise empty to use the embedded default starter.

Optional fields:

- Template source path.

Validation should happen before the review step. Invalid input should be shown in the UI without invoking external tools.

## Review Step

The review step should show:

- Project name.
- Full target project path.
- Output directory.
- Starter template.
- Template source.
- Prepared command when available.

The user must explicitly confirm before create runs.

## Execution

The UI should call the existing Core project creation behavior through a testable desktop-facing adapter or view model.

Execution state should include:

- Idle.
- Ready to review.
- Running.
- Succeeded.
- Failed.
- Canceled later if cancellation is added.

The first implementation does not need cancellation if it would complicate the command bridge boundary, but the state model should leave room for it.

## Logs

The UI should display:

- Prepared command.
- Standard output chunks.
- Standard error chunks.
- Final result summary.

Logs should be readable and copyable later, but copy/export is not required for the first milestone.

## Result Handling

On success:

- Show created project path.
- Show starter template result.
- Show next steps from the result where available.
- Refresh the workspace project list.
- Select or highlight the newly created project if practical.

On failure:

- Show validation errors, process errors, starter errors, or rollback result.
- Keep the create form values so the user can correct and retry.
- Do not hide logs.

## Safety

Because this is a mutating workflow:

- Do not run create immediately from the form.
- Always show a review step.
- Always require confirmation.
- Do not overwrite existing target project directories.
- Reuse Core validation and rollback behavior instead of duplicating it in the UI.
- Do not log secrets.

## Proposed Issues

### Define create project UI expectations

Acceptance criteria:

- `docs/ui-create-project.md` documents the create project UI flow.
- Mutating workflow safety rules are explicit.
- Proposed milestone issue set is documented.

### Add desktop create project form

Acceptance criteria:

- Workspace UI has a visible Create Project entry point.
- Form includes project name, output directory, starter template, and template source.
- Output directory defaults to the selected workspace.
- Template source defaults to the workspace catalog when present.
- Invalid or empty workspace states are handled clearly.

### Add create project review and confirmation step

Acceptance criteria:

- The UI shows target project path and create inputs before execution.
- The UI does not run create until the user confirms.
- Review state can return to edit mode.
- Tests cover the transition from form to review and back.

### Add create project execution view model

Acceptance criteria:

- The view model calls create through a testable abstraction.
- Prepared command, stdout, and stderr are captured into UI state.
- Running, success, and failure states are represented.
- Tests use a fake create service and do not invoke `npx`.

### Add create result handling and workspace refresh

Acceptance criteria:

- Success shows created project path and starter result.
- Failure shows validation, process, starter, or rollback information.
- Workspace discovery refreshes after successful create.
- The created project appears in the workspace list when the fake workspace contains it.

### Add create UI smoke tests

Acceptance criteria:

- Avalonia headless tests cover create form rendering.
- Review/confirmation rendering is smoke tested.
- Success and failure result states are smoke tested.
- Tests do not invoke real external tools.

## Open Technical Decisions

- Should the desktop create adapter call `ICreateProjectService` directly or wrap it with a desktop-specific workflow service?
- Should cancellation be implemented in the first create UI milestone or deferred?
- Should starter selection list only `mode: starter` templates from the workspace catalog, or allow manual template id entry first?
