# Desktop Command Center UI

This document defines the next desktop UI direction after workspace discovery and create project UI.

## Product Direction

The desktop app should become a local command center for mobile app work:

- Create and manage local React Native projects.
- Check whether the machine can create/build/run projects before mutation.
- Surface project health, setup gaps, template usage, publishing readiness, user traction, progress forecasts, and market/product work in one place.
- Keep risky actions explicit, reviewed, and auditable.

The UI should remain local-first. It should not become a remote SaaS dashboard or visual React Native screen builder unless that product direction is explicitly changed later.

## Immediate Pain Points

### Create Prompt Handling

React Native CLI can ask interactive questions during `npx @react-native-community/cli@latest init`, such as whether CocoaPods should be installed.

Current behavior:

- The prompt appears in the terminal process.
- The desktop UI can show logs but cannot answer the prompt.

Required behavior:

- The UI should avoid terminal-only prompts for the first implementation.
- The create workflow should expose a CocoaPods option before execution.
- The default should be conservative: do not install CocoaPods during create.
- The prepared command should include the non-interactive option or environment needed to prevent a hanging prompt where supported.
- If a future command still prompts, the UI should show a blocked/intervention state instead of appearing stuck.

### Doctor / Preflight

Before creating an app, the UI should show whether the machine has the required tools.

Existing CLI/Core behavior:

- `doctor` runs read-only dependency checks.
- Core has `IDependencyCheckService`, `DependencyCheckSummary`, and checks for core, Apple, and Android tools.
- `setup plan` builds guided remediation plans from the same dependency data.

Required UI behavior:

- Add a Doctor view backed by Core services, not by parsing CLI output.
- Show pass/warn/fail status for Node.js, npm, Git, Watchman, Xcode, CocoaPods, Java, Android SDK, and platform-specific tools.
- Show create readiness summary: Ready, Warnings, or Blocked.
- Link failed/warning checks to setup plan guidance.
- Let create review show a preflight warning when Doctor has failures.

### Full Application View

The app detail page should become an operational product view, not only a filesystem summary.

Required long-term areas:

- Local project status.
- Template usage and pending template opportunities.
- Doctor/setup health.
- Store metadata and publishing readiness.
- Release/build checklist state.
- Market/research notes and positioning.
- User acquisition, active user progress, growth assumptions, and future target projections.
- Visual charts that help answer whether the current trajectory is enough for the app's stated goals.
- Future tasks and next actions per app.

This should start with local data and explicit manual inputs. Remote market research, App Store Connect, Google Play Console, analytics, and competitor tracking should be designed as later optional integrations. Forecasts should be shown as calculations from local assumptions, not as promises or opaque predictions.

## Milestone Plan

### v1.4.0 - Desktop Readiness And Create Stabilization

Purpose:

Make the current desktop UI reliable enough for repeated local use.

Scope:

- Non-interactive create prompt handling.
- Doctor view in the desktop app.
- Setup plan preview in the desktop app.
- Create preflight status before execution.
- Navigation and scroll polish from dogfooding.
- Tests that do not require real external tools.

Suggested issues:

#### Define desktop readiness UI expectations

Acceptance criteria:

- This document captures prompt handling, Doctor, setup plan, and preflight behavior.
- The mutating create workflow rules are updated for non-interactive prompts.
- The next milestone issue set is documented.

#### Add non-interactive create prompt options

Acceptance criteria:

- Desktop create form exposes a CocoaPods install choice.
- Default behavior avoids CocoaPods install prompts.
- Prepared command or process request reflects the selected behavior where supported.
- Tests prove create does not rely on terminal-only input.

#### Add desktop Doctor view

Acceptance criteria:

- Desktop UI can run Doctor checks through `IDependencyCheckService`.
- Core, Apple, and Android dependency statuses are shown with pass/warn/fail states.
- The view reports overall create readiness.
- Tests use fake dependency checks.

#### Add setup plan preview to desktop UI

Acceptance criteria:

- Desktop UI can build setup guidance through `ISetupPlanService`.
- Manual, command, and environment steps are listed clearly.
- No setup command is executed from this issue.
- Tests use fake setup plan service.

#### Connect Doctor readiness to create review

Acceptance criteria:

- Create review shows the latest Doctor readiness state.
- Create can still run after explicit confirmation, but blocked checks are visible.
- The UI warns when Doctor has failures or has not been run.
- Tests cover ready, warning, and failed preflight states.

#### Add desktop navigation and scroll regression tests

Acceptance criteria:

- Headless tests cover Workspace, Templates, Create, Doctor, and Setup navigation entry points.
- Long template/create/project detail content remains scrollable.
- Back actions are visible in non-workspace views.

### v1.5.0 - Application Command Center

Purpose:

Turn the app detail page into a complete view of work needed for each mobile app.

Scope:

- App overview dashboard.
- Publishing readiness checklist.
- Store metadata and release checklist.
- Market/research notes as local structured data.
- Product work status and next actions.
- Template opportunities and project maintenance signals.

Suggested issues:

#### #219 Define application command center UI expectations

Acceptance criteria:

- This document captures the first command center scope and non-goals.
- Local-first metadata decisions are documented.
- The milestone issue set is created and linked.

#### #220 Define application command center data model

Acceptance criteria:

- Local metadata file contract is proposed for `.fabricator/app-command-center.json`.
- Publishing, research, release, and task sections are modeled.
- Existing `fabricator.json` compatibility is preserved.
- Missing metadata is represented explicitly and does not break existing projects.

#### #221 Add application command center overview shell

Acceptance criteria:

- App detail shows a command center section below existing overview data.
- Publishing, research, release, and next action panels have stable placeholders.
- Projects without command center metadata show empty states.
- The existing workspace and template detail behavior is preserved.

#### #222 Add publishing readiness panel

Acceptance criteria:

- App detail shows iOS and Android publishing readiness.
- Missing bundle id, application id, version, display name, icons, and release metadata are surfaced where detectable.
- Missing states are explicit and non-blocking.

#### #223 Add local market and research notes panel

Acceptance criteria:

- App detail can show market notes from local structured data.
- Competitors, keywords, target audience, positioning, and open research questions are represented.
- No remote research or scraping is required in the first implementation.

#### #224 Add release checklist panel

Acceptance criteria:

- App detail shows local release checklist state.
- Checklist items can be marked missing, ready, or unknown from best-effort detection.
- Manual notes are preserved in local metadata.

#### #225 Add next actions panel

Acceptance criteria:

- App detail shows recommended next actions from detected gaps.
- Doctor/setup/create/template/release gaps are grouped.
- The panel is deterministic and testable without network calls.

#### #226 Add application command center smoke tests

Acceptance criteria:

- Headless Desktop tests cover command center metadata present and missing states.
- Publishing, research, release checklist, and next action panels are asserted.
- Existing workspace, template, create, Doctor, and Setup navigation tests still pass.

## Technical Decisions

- Use Core services directly from Desktop view models where they already expose structured testable behavior.
- Do not parse CLI human output in the UI.
- Keep Doctor read-only.
- Keep setup execution out of the UI until setup plan preview and safety UX are stable.
- Treat market/research data as local manual metadata first.
- Store first-pass command center metadata in `.fabricator/app-command-center.json`.
- Keep `fabricator.json` focused on project identity and applied template state for now.
- Add remote integrations only after local contracts are stable.

## Planned Next Milestones

These milestones capture the intended direction after `v1.5.0 - Application Command Center`.
They are planning commitments, not frozen scope. In particular, the release workflow should be revisited after the command center editing and template apply workflows are dogfooded.

### v1.6.0 - Command Center Editing

Purpose:

Make `.fabricator/app-command-center.json` manageable from the desktop UI instead of only read from local files, and make the project detail page useful for app progress planning.

Scope:

- Create command center metadata from the UI when it is missing.
- Edit publishing metadata for iOS and Android.
- Edit market research notes, keywords, competitors, positioning, and open questions.
- Edit local project traction metrics such as acquired users, active users, growth targets, reporting cadence, and milestone progress.
- Show visual project intelligence panels for user growth, progress to target, required growth rate, and release/product readiness.
- Add, update, remove, and reorder release checklist items.
- Add, update, remove, and group next actions.
- Show validation, dirty state, save success, and save failure states.
- Keep all changes local-first and auditable.

Non-goals:

- No App Store Connect or Google Play Console sync yet.
- No third-party market intelligence provider yet.
- No Firebase, analytics SDK, product analytics, or revenue platform sync yet.
- No opaque AI forecast. Projection charts are deterministic calculations from user-entered metrics and assumptions.
- No template apply workflow yet.

#### Editing UX And Save Rules

When a user selects a project from the workspace list, the app detail page should open as a richer project workspace instead of a short filesystem summary. The first editable version should keep the page dense and operational:

- Header: project name, path, Fabricator state, command center metadata status, and last saved state.
- Project intelligence: local user traction metrics, target progress, projection summary, and visual charts.
- Publishing: iOS and Android metadata and publishing notes.
- Research: target audience, positioning, keywords, competitors, open questions, assumptions, and notes.
- Release checklist: ordered checklist items with status and notes.
- Next actions: grouped action items with status and notes.

Editing should happen inline inside the selected project detail page. Each section may have its own focused edit mode, but saving should write one coherent `.fabricator/app-command-center.json` document so the file stays auditable and easy to review in git.

Save behavior:

- Creating missing metadata writes a default local command center file before section edits.
- Saving writes only local metadata; it must not call remote services or mutate app source files.
- The UI shows dirty state as soon as an editable value differs from the loaded metadata.
- The UI shows save success with the saved metadata path and latest save time.
- Save failure keeps user-entered values in memory and shows the structured error.
- Navigating away with unsaved changes must require an explicit discard or stay decision.

Validation states:

- Required identifiers such as checklist item IDs and next action IDs cannot be empty.
- List item IDs must be unique within their section.
- Metric values cannot be negative.
- Active users cannot exceed acquired users for the same snapshot.
- Target users must be greater than or equal to the latest acquired users unless the target is already reached.
- Target date must be after the latest metric snapshot date for forward projection.
- A projection needs at least two metric snapshots; otherwise the page shows an insufficient history state.

Project intelligence should start from manual local metrics:

- `targetUsers`: the desired acquired user count.
- `targetDate`: the date the target should be reached.
- `reportingCadence`: weekly or monthly for the first UI.
- `metricSnapshots`: dated rows containing acquired users, active users, optional retention proxy, and notes.
- `milestoneProgress`: local product progress items such as MVP, beta, store assets, launch, or post-launch iteration.
- `assumptions`: short notes that explain why the target or growth expectation is reasonable.

Projection calculations should be deterministic:

- Current acquired users are taken from the latest snapshot.
- Current growth is calculated from the first and latest snapshots over the selected cadence.
- Required growth is `(target users - latest acquired users) / remaining cadence periods`.
- The projection status is `Reached`, `On track`, `At risk`, `Insufficient history`, or `No target`.
- `On track` means current average growth is greater than or equal to required growth.

Visual project intelligence panels should include:

- User acquisition trend: line or compact area chart from dated acquired user snapshots.
- Active user trend: line chart or overlay from active user snapshots.
- Progress to target: progress bar or radial summary using latest acquired users and target users.
- Required growth: comparison of current average growth versus required growth.
- Product progress: compact checklist or segmented progress view for milestone progress.

The visual design should stay consistent with a developer desktop tool. Charts should support empty states and compact labels, avoid decorative marketing visuals, and remain testable through view model values and Avalonia headless smoke tests.

Planned issues:

#### #235 Define command center editing UX and save rules

Acceptance criteria:

- This document captures the editing UX, save behavior, validation states, and non-goals.
- The UI distinguishes missing metadata, unsaved changes, saved state, validation errors, save failures, missing metric data, and insufficient metric history.
- The first editable fields are listed for publishing, research, project metrics, release checklist, and next actions.
- Project detail UX includes charts for acquired users, active users or retention proxy, progress to target, and required growth to reach a future goal.
- Projection language makes clear that the calculation uses local assumptions and does not imply guaranteed growth.

#### #244 Add project intelligence metadata model

Acceptance criteria:

- The command center metadata contract includes a local project intelligence section.
- The section can represent metric snapshots, target users, target date, reporting cadence, product milestone progress, and assumptions.
- Missing or empty metric data is represented explicitly and does not break existing command center files.
- Tests cover loading populated, missing, empty, and partially specified project intelligence metadata.

#### #237 Add command center metadata write service

Acceptance criteria:

- Core can write `.fabricator/app-command-center.json` using the command center schema.
- Writes preserve supported fields and normalize missing collections, including project intelligence metrics.
- Invalid project paths and invalid metadata return structured errors.
- Tests cover create, update, malformed existing file, and invalid path cases.

#### #236 Add command center metadata create flow

Acceptance criteria:

- App detail shows a clear action when command center metadata is missing.
- The UI can create a default `.fabricator/app-command-center.json`.
- The default metadata includes empty publishing, research, project intelligence, release checklist, and next action sections.
- The created file is immediately loaded into the selected project detail.
- Tests cover missing metadata to loaded metadata transition.

#### #238 Add publishing metadata edit form

Acceptance criteria:

- UI can edit iOS and Android display names, identifiers, versions, build numbers, statuses, store URLs, release owner, and notes.
- Save writes local metadata only.
- Missing values remain visible after save.
- Tests cover field binding, save, and reload.

#### #241 Add market research edit form

Acceptance criteria:

- UI can edit target audience, positioning, keywords, competitors, open questions, growth assumptions, and notes.
- List fields support add/remove behavior.
- Save writes local metadata only.
- Tests cover add, remove, save, and reload.

#### #245 Add project metrics edit form

Acceptance criteria:

- UI can edit acquired users, active users, target users, target date, reporting cadence, and product milestone progress.
- UI can add and remove dated metric snapshots.
- Invalid metric values, invalid dates, and impossible target calculations are shown before save.
- Save writes local metadata only.
- Tests cover empty, add snapshot, update snapshot, remove snapshot, save, and reload.

#### #246 Add project intelligence charts

Acceptance criteria:

- App detail shows visual charts for user acquisition over time, active user or retention proxy over time, progress toward target users, and required growth rate to hit the target date.
- Charts have explicit empty states when there is no metric history.
- Calculations are deterministic and testable without network calls.
- The UI remains readable in the project detail page without turning the desktop app into a marketing dashboard.
- Tests cover populated charts, empty states, and projection summary text.

#### #239 Add release checklist CRUD

Acceptance criteria:

- UI can add, edit, delete, and reorder release checklist items.
- Each item supports title, status, and notes.
- The release workflow design remains explicitly revisitable after dogfooding.
- Tests cover empty, add, update, delete, reorder, save, and reload.

#### #240 Add next actions CRUD

Acceptance criteria:

- UI can add, edit, delete, and group next actions.
- Each action supports title, group, status, and notes.
- The panel remains local-first and deterministic.
- Tests cover empty, add, update, delete, save, and reload.

#### #242 Add command center editing dirty state and validation

Acceptance criteria:

- UI shows unsaved changes before save.
- UI prevents invalid saves where required fields are missing.
- UI shows save success and save failure states.
- Navigation away from unsaved changes is handled explicitly.
- Tests cover dirty, invalid, success, failure, and invalid metric projection states.

#### #243 Add command center editing smoke tests

Acceptance criteria:

- Headless Desktop tests cover create metadata, edit publishing, edit research, edit project metrics, project intelligence charts, edit release checklist, and edit next actions.
- Existing read-only command center smoke tests continue to pass.
- Full solution tests pass without network calls.

### v1.7.0 - Template Apply UI

Purpose:

Let users apply reusable Fabricator templates to existing local React Native projects from the desktop UI.

Scope:

- Select a target project.
- Browse grouped templates.
- Preview the planned file and state changes before mutation.
- Apply a selected template.
- Show logs and result state.
- Record applied template state in the project.

Non-goals:

- No visual React Native screen builder.
- No remote template marketplace.

### v1.8.0 - Release Readiness Workflow

Purpose:

Turn the command center into a practical release preparation assistant for each mobile app.

Provisional scope:

- Release checklist templates.
- Version and build number consistency checks.
- Store metadata completeness checks.
- Icon, screenshots, privacy policy, and platform config readiness.
- A local "ready to submit" summary.
- Links from failed readiness checks to command center editing or setup guidance.

Open planning note:

This milestone is intentionally provisional. The exact release workflow should be updated after `v1.6.0` and `v1.7.0` are tested locally, because command center editing and template apply behavior will shape what a useful release workflow should automate versus merely track.

## Open Questions

- Should CocoaPods install be a simple yes/no create option or a later post-create action?
- Should Doctor run automatically when a workspace is loaded, or only when the user clicks Run Doctor?
- Where should application command center metadata live: inside `fabricator.json`, a separate `.fabricator/app-workspace.json`, or both?
- Which market/research fields are mandatory enough to become schema fields versus freeform notes?
