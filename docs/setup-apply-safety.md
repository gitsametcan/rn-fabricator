# Setup Apply Safety Policy

This document defines the safety rules for `rn-fabricator setup apply` execution. The `setup plan` command remains read-only.

## Goals

- Never mutate a user's machine without explicit intent.
- Show the exact command before it runs.
- Ask for confirmation per executable step by default.
- Keep GUI installers, elevated commands, license acceptance, and shell profile edits out of automatic execution in the first implementation.
- Make failures visible without hiding remaining manual work.

## Step Classifications

Each setup plan item must be classified before execution:

- `safe command`: A low-risk command that installs a tool through a detected package manager without elevation.
- `elevated command`: A command that requires `sudo`, administrator privileges, system-level changes, or license acceptance.
- `manual action`: A GUI install, account login, license flow, or user decision that the CLI cannot safely perform.
- `environment change`: A shell profile, PATH, `ANDROID_HOME`, or machine/user environment variable update.
- `unsupported action`: A step that rn-fabricator can describe but must not execute on the current platform.

Only `safe command` steps are eligible for execution in the first `setup apply` implementation.

## Default Interactive Behavior

`rn-fabricator setup apply` should:

1. Build the same plan as `rn-fabricator setup plan`.
2. Print platform, package manager, toolchain profile, and React Native version.
3. Print every step, including skipped manual and environment steps.
4. Prompt before each executable safe command.
5. Use `y/N` with No as the default.
6. Continue to the next step after a declined command.
7. Continue through the plan after a failed command and return a failure exit code at the end.
8. Print a final summary of succeeded, failed, skipped by user, skipped by policy, and manual-only steps.

Prompt format:

```text
[command] Watchman
Command: brew install watchman
Run this command? [y/N]:
```

Accepted input:

- `y`
- `yes`

Any other input, including empty input, must skip the command.

## Dry Run

`rn-fabricator setup apply --dry-run` must:

- Build and print the plan.
- Print which commands would be eligible for execution.
- Print which steps would be skipped by policy.
- Execute nothing.
- Exit successfully if the plan can be built.

`--dry-run` is the safest documented path for users who want to inspect behavior.

## Yes Mode

`rn-fabricator setup apply --yes` must:

- Execute only `safe command` steps.
- Skip `elevated command`, `manual action`, `environment change`, and `unsupported action` steps.
- Print each skipped step with a reason.
- Never imply that the environment is fully configured when manual or skipped steps remain.

`--yes` must not run `sudo`, edit shell profiles, install Xcode, install Android Studio through a GUI, accept licenses, or modify machine-level environment variables in the first implementation.

If `--yes` and `--dry-run` are both provided, the command should fail with invalid input unless a future issue explicitly defines combined semantics.

## First Implementation Allowlist

The first `setup apply` implementation executes only commands matching an explicit allowlist.

Initial macOS allowlist:

- `brew install watchman`
- `brew install git`
- `brew install node`
- `brew install --cask temurin`

Initial Windows allowlist:

- `winget install OpenJS.NodeJS.LTS`
- `winget install Git.Git`
- `winget install EclipseAdoptium.Temurin.17.JDK`

Initial Linux allowlist:

- None by default, because apt commands normally require `sudo`.

Commands not on the allowlist must be skipped by policy, even if they appear in a setup plan.

## Explicitly Manual In First Implementation

These steps must remain manual:

- Installing Xcode from the App Store.
- Selecting Xcode with `xcode-select`.
- Running Xcode first launch or license acceptance.
- Installing Android Studio.
- Installing Android SDK packages through Android Studio SDK Manager.
- Editing shell profile files such as `.zshrc`, `.bashrc`, or PowerShell profiles.
- Setting machine-level or user-level environment variables.
- Running `sudo gem install cocoapods`.
- Running any command that prompts for administrator credentials.

## Failure Reporting

For each executed command, capture:

- Command text.
- Exit code.
- Short stdout summary when useful.
- Short stderr summary when useful.

The final summary should group results:

- `succeeded`
- `failed`
- `skipped by user`
- `skipped by policy`
- `manual`

Failure output should explain that skipped manual and environment steps may still be required before React Native development works.

## Platform Notes

### macOS

Homebrew commands can be safe when Homebrew is detected and the command is allowlisted. Xcode, Android Studio, shell profile edits, and `sudo` commands remain manual.

### Windows

Winget commands can be safe when winget is detected and the command is allowlisted. Administrator prompts, machine-level environment variables, and GUI installers remain manual.

### Linux

Linux package managers vary by distribution. The first implementation should treat `sudo apt-get ...` as elevated and skip it by policy, even when it appears in the plan.

## Non-Goals

- Fully configuring a React Native machine without user involvement.
- Bypassing OS permission prompts.
- Editing shell profiles automatically.
- Installing GUI applications automatically.
- Guaranteeing that every third-party installer is safe or non-interactive.
