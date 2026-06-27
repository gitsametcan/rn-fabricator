# Dogfooding Workflow

Dogfooding means testing rn-fabricator like a real user, outside this repository, before turning feedback into issues.

## Playground Location

Use a repo-external playground directory:

```bash
mkdir -p ~/Documents/rn-fabricator-playground
cd ~/Documents/rn-fabricator-playground
```

Do not generate React Native projects inside the rn-fabricator repository. Generated apps contain many files and can pollute the tool repository's git status.

## Install The Latest Release Package

Download the release package into the playground:

```bash
curl -L -o rn-fabricator.1.0.0-beta.2.nupkg \
  https://github.com/gitsametcan/rn-fabricator/releases/download/v1.0.0-beta.2/rn-fabricator.1.0.0-beta.2.nupkg
```

Install it as a local tool:

```bash
dotnet tool install rn-fabricator \
  --tool-path ./.tools \
  --add-source . \
  --version 1.0.0-beta.2
```

Verify the installed tool:

```bash
./.tools/rn-fabricator --version
./.tools/rn-fabricator --help
```

## Replace An Installed Playground Tool

Uninstall the current local tool:

```bash
dotnet tool uninstall rn-fabricator --tool-path ./.tools
```

Install a locally packed development version from the rn-fabricator repository:

```bash
dotnet tool install rn-fabricator \
  --tool-path ./.tools \
  --add-source /Users/sametcan/Documents/GitHub/fabricator/artifacts/packages \
  --version 1.0.0-beta.2
```

Use a version override when packing an experimental build:

```bash
cd /Users/sametcan/Documents/GitHub/fabricator
dotnet pack src/Fabricator.Cli/Fabricator.Cli.csproj \
  --configuration Release \
  --output artifacts/packages \
  -p:VersionPrefix=1.0.0 \
  -p:VersionSuffix=dogfood.1
```

Then install that version from the playground:

```bash
cd ~/Documents/rn-fabricator-playground
dotnet tool install rn-fabricator \
  --tool-path ./.tools \
  --add-source /Users/sametcan/Documents/GitHub/fabricator/artifacts/packages \
  --version 1.0.0-dogfood.1
```

## Run Dogfooding Checks

Run the diagnostic command:

```bash
./.tools/rn-fabricator doctor
```

Preview guided setup actions without installing dependencies. This command is read-only; it should print the platform, package manager, selected toolchain profile, React Native version, and planned steps:

```bash
./.tools/rn-fabricator setup plan
```

Preview setup execution without prompts or installs. Start here when testing setup behavior on a machine you do not want to mutate:

```bash
./.tools/rn-fabricator setup apply --dry-run
```

Expected dry-run behavior:

- The setup plan is printed first.
- Safe allowlisted commands are shown as `[dry-run] ... would run ...`.
- Manual, elevated, and environment steps are not executed.
- The final summary can include `would run`, `skipped by policy`, and `manual` counts.

Run safe setup commands only after per-step confirmation:

```bash
./.tools/rn-fabricator setup apply
```

Expected prompt format:

```text
[command] Watchman
Command: brew install watchman
Run this command? [y/N]:
```

Answer `y` or `yes` only when you want that command to run. Press Enter, type `n`, or type anything else to skip the command and continue the plan.

Run only safe allowlisted setup commands without prompts. Use this only after reviewing `setup apply --dry-run`:

```bash
./.tools/rn-fabricator setup apply --yes
```

`--yes` does not run `sudo` commands, install Xcode or Android Studio, edit shell profiles, accept licenses, or configure Android SDK environment variables.

## Template Catalog Dogfooding

For the end-to-end reusable template user workflow, see [Reusable Template Workflow](reusable-template-workflow.md). This dogfooding section keeps the same flow in a playground-oriented checklist form.

Use the local repository catalog while testing unreleased template behavior:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="/Users/sametcan/Documents/GitHub/fabricator/templates/catalog.fabricator.json"
```

Keep dogfooding outside the rn-fabricator repository so generated React Native files and local template experiments do not pollute the source tree:

```bash
mkdir -p ~/Documents/rn-fabricator-playground/local-templates
cd ~/Documents/rn-fabricator-playground
export RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE="$PWD/local-templates/catalog.fabricator.json"
```

Use the GitHub raw catalog only after the relevant changes are pushed:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json"
```

Use the release-tagged raw catalog when validating a beta package exactly as users will see it:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="https://raw.githubusercontent.com/gitsametcan/rn-fabricator/v1.0.0-beta.2/templates/catalog.fabricator.json"
```

Create a sample React Native project with the minimal splash and main starter:

```bash
./.tools/rn-fabricator create FabricatorBabyStep \
  --output . \
  --template-source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Expected generated starter files:

```text
FabricatorBabyStep/App.tsx
FabricatorBabyStep/.fabricator/project.json
FabricatorBabyStep/fabricator.json
FabricatorBabyStep/src/screens/SplashScreen.tsx
FabricatorBabyStep/src/screens/MainScreen.tsx
FabricatorBabyStep/src/screens/index.ts
FabricatorBabyStep/src/app/index.ts
FabricatorBabyStep/src/components/index.ts
FabricatorBabyStep/src/config/index.ts
FabricatorBabyStep/src/constants/index.ts
FabricatorBabyStep/src/hooks/index.ts
FabricatorBabyStep/src/services/index.ts
FabricatorBabyStep/src/storage/index.ts
FabricatorBabyStep/src/theme/index.ts
FabricatorBabyStep/src/types/index.ts
FabricatorBabyStep/src/utils/index.ts
```

List templates from the local Fabricator template catalog:

```bash
./.tools/rn-fabricator templates list \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

List templates by category:

```bash
./.tools/rn-fabricator templates list \
  --category auth \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Expected list output includes:

```text
basic-auth
component/primary-button
layout/app-shell
minimal-splash
screen/main-menu
service/api-client
util/storage
```

Expected category output includes `basic-auth` and excludes `minimal-splash`.

Validate the catalog before applying templates:

```bash
./.tools/rn-fabricator templates validate \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Expected validate output includes:

```text
Template catalog validation
Result: valid
Summary: catalog is valid.
```

List component templates:

```bash
./.tools/rn-fabricator templates list \
  --category component \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Expected component category output includes `component/primary-button`.

Inspect a template before applying or copying it:

```bash
./.tools/rn-fabricator templates info basic-auth \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Expected info output includes:

```text
Template: basic-auth (0.1.0)
Category: auth
Files: 11
Exports: 4
Integration hints: 2
```

Inspect a reusable example template:

```bash
./.tools/rn-fabricator templates info component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Expected reusable example info output includes:

```text
Template: component/primary-button (0.1.0)
Category: component
Files: 1
Exports: 1
```

Apply an optional template into the generated project:

```bash
./.tools/rn-fabricator templates apply basic-auth \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep
```

Expected applied files:

```text
FabricatorBabyStep/.env.example
FabricatorBabyStep/credentials.example.json
FabricatorBabyStep/CONFIGURATION.md
FabricatorBabyStep/App.tsx
FabricatorBabyStep/src/auth/AuthProvider.tsx
FabricatorBabyStep/src/auth/index.ts
FabricatorBabyStep/src/screens/SplashScreen.tsx
FabricatorBabyStep/src/screens/LoadingScreen.tsx
FabricatorBabyStep/src/screens/LoginScreen.tsx
FabricatorBabyStep/src/screens/HomeScreen.tsx
FabricatorBabyStep/src/screens/index.ts
```

Expected apply output includes export and integration sections:

```text
State tracking: fabricator.json updated
Exports applied: 4
Integration notes: 2
Summary:
```

`src/screens/index.ts` should include idempotent screen exports such as:

```ts
export { HomeScreen } from './HomeScreen';
```

Run the same apply command again to verify non-destructive behavior:

```bash
./.tools/rn-fabricator templates apply basic-auth \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep
```

Expected repeat-apply behavior:

- Exit code stays successful.
- Existing files are reported under `Skipped`.
- Existing files are not overwritten.
- Existing barrel exports are reported under `Exports skipped`.

Use overwrite only after reviewing skipped files:

```bash
./.tools/rn-fabricator templates apply basic-auth \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep \
  --overwrite
```

Apply a reusable example template:

```bash
./.tools/rn-fabricator templates apply component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep
```

Expected generated file:

```text
FabricatorBabyStep/src/components/PrimaryButton.tsx
```

`src/components/index.ts` should include:

```ts
export { PrimaryButton } from './PrimaryButton';
```

Check the applied template history:

```bash
./.tools/rn-fabricator templates status \
  --project ./FabricatorBabyStep
```

Expected status output includes the create operation and later apply operations from `fabricator.json`.

Capture a reusable template from the generated project's screen folder:

```bash
./.tools/rn-fabricator templates capture baby-step-screens \
  --category screens \
  --from ./FabricatorBabyStep \
  --output ./captured-templates
```

Expected captured template files include:

```text
captured-templates/baby-step-screens/fabricator-template.json
captured-templates/baby-step-screens/src/screens/index.ts
```

The generated manifest should include `category: screens` and file mappings with `targetFolder: screens`.

Capture only one screen from the generated project:

```bash
./.tools/rn-fabricator templates capture fabricator-beta-screen \
  --category screens \
  --from ./FabricatorBabyStep \
  --output ./captured-templates \
  --include src/screens/FabricatorBeta.tsx
```

Expected selected-file capture output includes only the requested screen and the template manifest:

```text
captured-templates/fabricator-beta-screen/fabricator-template.json
captured-templates/fabricator-beta-screen/src/screens/FabricatorBeta.tsx
```

Add and register a local reusable template:

```bash
./.tools/rn-fabricator templates add baby-step-screens \
  --category screens \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --dry-run
```

Expected dry-run behavior:

- No `local-templates/catalog.fabricator.json` file is written.
- No `local-templates/baby-step-screens` folder is created.
- Output includes `Template add dry-run`, `Files to capture`, and `Summary`.

Drop `--dry-run` after reviewing the output:

```bash
./.tools/rn-fabricator templates add baby-step-screens \
  --category screens \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE"
```

Add and register only one selected screen:

```bash
./.tools/rn-fabricator templates add fabricator-beta-screen \
  --category screens \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --include src/screens/FabricatorBeta.tsx
```

Validate and inspect the local catalog:

```bash
./.tools/rn-fabricator templates validate \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE"

./.tools/rn-fabricator templates info baby-step-screens \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE"
```

Update the local template after changing files under `FabricatorBabyStep/src/screens`:

```bash
./.tools/rn-fabricator templates update baby-step-screens \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --dry-run
```

Drop `--dry-run` only after reviewing added, changed, removed, and unchanged file counts.

Update only the selected-screen template after changing `FabricatorBeta.tsx`:

```bash
./.tools/rn-fabricator templates update fabricator-beta-screen \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --include src/screens/FabricatorBeta.tsx \
  --dry-run
```

Remove a local template entry without deleting its folder:

```bash
./.tools/rn-fabricator templates remove baby-step-screens \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --dry-run
```

Use `--delete-files` only when the local template folder should also be deleted.

Known manual integration limits to watch during dogfooding:

- Fabricator can add safe barrel exports, but it does not edit arbitrary imports in user-authored files.
- Navigation registration, menu wiring, dependency installation, and secret or environment decisions remain manual integration notes.
- Remote GitHub raw catalogs are read-only; use a local filesystem catalog for add, update, and remove.

Use low-level copy only when you intentionally want to copy template source paths without Fabricator project validation:

```bash
./.tools/rn-fabricator templates copy basic-auth \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep
```

Inspect the generated project:

```bash
cd FabricatorBabyStep
ls
```

When the generated app is no longer needed, remove it from the playground:

```bash
cd ~/Documents/rn-fabricator-playground
rm -rf FabricatorBabyStep
```

## Setup Troubleshooting Notes

### Declined Commands

If you decline a prompt, the command is recorded as skipped by user. This is expected and should not fail the whole run.

Run the command again when you want to approve a skipped step:

```bash
./.tools/rn-fabricator setup apply
```

### Missing Homebrew

On macOS, if Homebrew is missing, `setup plan` falls back to manual guidance instead of printing `brew install ...` commands.

Install Homebrew manually from https://brew.sh, restart the terminal, then run:

```bash
./.tools/rn-fabricator setup plan
```

### Sudo Commands

When Homebrew is available on macOS, `setup plan` should prefer:

```bash
brew install cocoapods
```

That command is eligible for `setup apply` confirmation. Commands such as `sudo gem install cocoapods` are skipped by policy in `setup apply` and `setup apply --yes`.

Review the printed command and run it manually only when you understand the system-level change:

```bash
sudo gem install cocoapods
```

### Android SDK Manual Steps

Android SDK setup remains manual in the guided setup flow.

Use Android Studio SDK Manager to install the required SDK packages from the plan output.

On macOS, open SDK Manager in one of these ways:

- From the Android Studio welcome screen: `More Actions > SDK Manager`.
- If a project is open: `Android Studio > Settings > Languages & Frameworks > Android SDK`.

For the default macOS zsh shell, open your shell profile:

```bash
nano ~/.zshrc
```

Add these lines at the bottom:

```bash
export ANDROID_HOME="$HOME/Library/Android/sdk"
export PATH="$PATH:$ANDROID_HOME/platform-tools"
```

Save and exit nano with `Ctrl+O`, `Enter`, then `Ctrl+X`.

Restart the terminal or load the profile in the current terminal:

```bash
source ~/.zshrc
```

Verify:

```bash
echo "$ANDROID_HOME"
adb --version
```

## Feedback To Issue Workflow

Capture feedback as short, concrete observations:

```text
doctor: Missing Java hint should mention Temurin on macOS.
create: Failure output should show the exact command that failed.
template: Login screen needs loading state while sign-in is running.
docs: Playground install steps should mention tool uninstall first.
```

Turn each observation into a GitHub issue with:

- A user-facing title.
- The command that was run.
- The expected behavior.
- The actual behavior.
- The platform and tool version.
- Screenshots or terminal output when useful.

Assign issues to the active feedback milestone and label them by area, for example `area:cli`, `area:templates`, or `area:release`.

## Before Opening A Pull Request

For any dogfooding fix, run:

```bash
dotnet build rn-fabricator.sln --configuration Release
dotnet test rn-fabricator.sln --no-build --configuration Release
dotnet pack src/Fabricator.Cli/Fabricator.Cli.csproj --no-build --configuration Release --output artifacts/packages
```

If the fix changes user-facing behavior, reinstall the packed tool in the playground and repeat the relevant command.
