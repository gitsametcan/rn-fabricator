# Guided Setup Design

This document defines how rn-fabricator should help users move from `doctor` diagnostics to dependency setup without running unsafe automatic installs.

## Decision

Use a separate `setup` command family instead of `doctor --fix` or `doctor install`.

Planned commands:

```text
rn-fabricator setup plan
rn-fabricator setup apply
```

`doctor` remains a read-only diagnostic command. `setup` becomes the explicit place for guided installation plans and, later, user-confirmed install execution.

## Why Not `doctor --fix`

`doctor --fix` sounds convenient, but it hides too much risk:

- Users may expect it to mutate their machine immediately.
- Installing Xcode, Android Studio, Java, CocoaPods, and Watchman can require admin access, GUI steps, license acceptance, or shell profile changes.
- The same failed check can need different actions depending on platform, package manager, and user preference.

Keeping `doctor` read-only makes it safe to run in CI, scripts, and user terminals.

## Command Behavior

### `setup plan`

`setup plan` should:

- Run the same dependency checks as `doctor`.
- Build a platform-specific setup plan for missing or warning dependencies.
- Select the default stable React Native toolchain profile unless a profile or React Native version is provided.
- Print the selected profile and React Native version near the top of the plan.
- Include profile-backed version expectations, such as Node LTS, Java supported range, Xcode minimum version, CocoaPods minimum version, and Android SDK values.
- Print commands and manual steps without executing them.
- Mark each step as `manual`, `command`, or `environment`.
- Explain which steps require admin privileges or GUI interaction.

Example:

```text
React Native setup plan

Toolchain profile: React Native Stable (react-native-stable)
React Native: 0.76.x

[manual] Xcode
  Install Xcode from the App Store.
  Profile recommendation for React Native 0.76.x: Xcode minimum version 15.0.
  Then run: sudo xcode-select --switch /Applications/Xcode.app

[command] Watchman
  brew install watchman
  Profile recommendation for React Native 0.76.x: Watchman latest stable version.

[environment] Android SDK
  export ANDROID_HOME="$HOME/Library/Android/sdk"
  export PATH="$PATH:$ANDROID_HOME/platform-tools"
```

Profile selection examples:

```bash
rn-fabricator setup plan
rn-fabricator setup plan --profile react-native-stable
rn-fabricator setup plan --react-native 0.76.x
```

Unsupported profile requests should fail before rendering the plan and explain which profile or React Native version is unsupported.

### Toolchain Profiles

Toolchain profiles keep setup guidance tied to a tested React Native environment instead of blindly recommending the latest version of every dependency.

The first local profile is `react-native-stable`. It contains:

- React Native version family.
- Node.js recommendation strategy.
- Java supported range.
- Xcode and CocoaPods minimum versions.
- Watchman recommendation.
- Android SDK compile, target, min SDK, and required package ids.

Profile data is local and deterministic in the first implementation. Remote profile updates can be considered later, but setup planning should remain usable without network access.

### `setup apply`

`setup apply` applies only safe allowlisted command steps after explicit user confirmation.

It must:

- Show the setup plan before execution.
- Require per-step `y/N` confirmation before running executable commands.
- Support `--dry-run` for non-mutating execution previews.
- Support `--yes` only for safe allowlisted commands.
- Skip manual, elevated, environment, and unsupported steps by policy.
- Print a final execution summary.

Initial execution is conservative:

```bash
rn-fabricator setup apply
rn-fabricator setup apply --dry-run
rn-fabricator setup apply --yes
```

Avoid silently editing shell profile files in the first implementation. Print environment variable snippets and let users apply them.

Detailed execution rules are defined in [Setup Apply Safety Policy](setup-apply-safety.md).

## Dogfooding Workflow

Test guided setup from a repo-external playground directory, not from the rn-fabricator repository. Generated React Native apps and local tool installs should stay outside the tool repository so `git status` remains clean.

Recommended order:

```bash
rn-fabricator doctor
rn-fabricator setup plan
rn-fabricator setup apply --dry-run
rn-fabricator setup apply
```

Use `setup apply --yes` only after reviewing `setup apply --dry-run`. Even in yes mode, elevated commands, manual GUI installs, and environment changes remain skipped by policy.

Detailed repo-external testing steps are defined in [Dogfooding Workflow](dogfooding.md).

## Future Work: Deeper Environment Automation

The current `setup apply` flow is intentionally conservative. It can run safe allowlisted commands after confirmation, while elevated commands, GUI installers, license prompts, and shell profile changes remain manual.

Future versions may explore deeper environment automation without assigning it to a current milestone. Potential directions include:

- Opt-in elevated command execution for supported commands.
- Shell profile updates with backup and restore support.
- Android SDK command-line tools installation.
- CocoaPods installation automation.
- Post-install `doctor` verification loops.
- Rollback behavior for files modified by setup automation.
- Clearer boundaries for Xcode, Android Studio, and other GUI or license-gated installers.

These improvements should be designed as explicit opt-in behavior with clear prompts, auditable commands, and platform-specific safety rules.

## Confirmation Rules

Before any install command runs, the CLI should print:

- Platform.
- Package manager or tool being used.
- Exact commands that will run.
- Steps that will remain manual.
- A clear prompt.

Example prompt:

```text
[command] Watchman
Command: brew install watchman
Run this command? [y/N]:
```

Only `y` and `yes` should approve execution. Empty input or any other value should skip the command.

## Platform Boundaries

### macOS

Supported guidance:

- Homebrew commands for Node.js, Git, Watchman, and Temurin.
- CocoaPods install guidance.
- Xcode manual install and `xcode-select` commands.
- Android Studio install guidance.
- `ANDROID_HOME="$HOME/Library/Android/sdk"` environment snippet.

Execution boundaries:

- Do not install Xcode automatically.
- Do not accept Xcode licenses automatically.
- Do not edit shell profiles automatically in the first implementation.
- Treat `sudo` commands as manual or require a separate explicit design.

### Windows

Supported guidance:

- `winget` commands for Node.js, Git, Temurin, and Android Studio where available.
- Android SDK environment variable guidance for `%LOCALAPPDATA%\Android\Sdk`.
- PowerShell verification commands.

Execution boundaries:

- Do not run elevated commands automatically.
- Do not edit machine-level environment variables automatically in the first implementation.
- Watchman should remain optional unless a future Windows workflow requires it.

### Linux

Supported guidance:

- Distribution package manager hints, with Ubuntu examples for apt.
- Android Studio manual install guidance.
- `ANDROID_HOME="$HOME/Android/Sdk"` environment snippet.

Execution boundaries:

- Do not assume every Linux distribution uses apt.
- Do not run `sudo` commands automatically in the first implementation.
- Prefer plan output over command execution until package manager detection is implemented.

## Package Manager Detection

Initial detection is intentionally read-only:

- macOS checks `brew --version` and uses Homebrew command guidance when available.
- Windows checks `winget --version` and uses winget command guidance when available.
- Linux checks `apt-get --version` and uses Ubuntu-style apt guidance when available.

If the expected package manager is not available, the setup plan falls back to manual guidance instead of treating the missing package manager as a setup failure.

## Implementation Sequence

The design should be implemented in small increments:

1. Add setup plan model and renderer.
2. Add `setup plan` command.
3. Add package manager detection.
4. Add local toolchain profiles.
5. Use toolchain profiles in `setup plan` recommendations.
6. Add guarded setup execution dry-run behavior.
7. Add interactive setup execution for low-risk commands.

Each increment should include tests and dogfooding notes.

Initial implementation issues:

- #59 Add setup plan model and renderer.
- #58 Add setup plan command.
- #57 Design package manager detection for setup.
- #64 Define React Native toolchain profile model.
- #65 Add local toolchain profile data source.
- #66 Use toolchain profiles in setup plan recommendations.
- #67 Design setup apply safety policy.
