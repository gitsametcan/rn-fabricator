# Desktop Command Center UI

This document defines the next desktop UI direction after workspace discovery and create project UI.

## Product Direction

The desktop app should become a local command center for mobile app work:

- Create and manage local React Native projects.
- Check whether the machine can create/build/run projects before mutation.
- Surface project health, setup gaps, template usage, publishing readiness, and market/product work in one place.
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
- Future tasks and next actions per app.

This should start with local data and explicit manual inputs. Remote market research, App Store Connect, Google Play Console, analytics, and competitor tracking should be designed as later optional integrations.

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

#### Define application command center data model

Acceptance criteria:

- Local metadata file contract is proposed.
- Publishing, research, release, and task sections are modeled.
- Existing `fabricator.json` compatibility is preserved.

#### Add publishing readiness panel

Acceptance criteria:

- App detail shows iOS and Android publishing readiness.
- Missing bundle id, application id, version, display name, icons, and release metadata are surfaced where detectable.
- Missing states are explicit and non-blocking.

#### Add local market and research notes panel

Acceptance criteria:

- App detail can show market notes from local structured data.
- Competitors, keywords, target audience, positioning, and open research questions are represented.
- No remote research or scraping is required in the first implementation.

#### Add release checklist panel

Acceptance criteria:

- App detail shows local release checklist state.
- Checklist items can be marked missing, ready, or unknown from best-effort detection.
- Manual notes are preserved in local metadata.

#### Add next actions panel

Acceptance criteria:

- App detail shows recommended next actions from detected gaps.
- Doctor/setup/create/template/release gaps are grouped.
- The panel is deterministic and testable without network calls.

## Technical Decisions

- Use Core services directly from Desktop view models where they already expose structured testable behavior.
- Do not parse CLI human output in the UI.
- Keep Doctor read-only.
- Keep setup execution out of the UI until setup plan preview and safety UX are stable.
- Treat market/research data as local manual metadata first.
- Add remote integrations only after local contracts are stable.

## Open Questions

- Should CocoaPods install be a simple yes/no create option or a later post-create action?
- Should Doctor run automatically when a workspace is loaded, or only when the user clicks Run Doctor?
- Where should application command center metadata live: inside `fabricator.json`, a separate `.fabricator/app-workspace.json`, or both?
- Which market/research fields are mandatory enough to become schema fields versus freeform notes?
