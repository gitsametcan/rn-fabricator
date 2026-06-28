# Reusable Template Workflow

This guide shows the end-to-end workflow for reusing React Native project work across Fabricator-created projects.

## When To Use This

Use this workflow when you have a Fabricator-compatible project and want to:

- Discover reusable templates from a catalog.
- Apply a reusable screen, component, service, utility, or layout to a project.
- Capture work from one Fabricator project as a reusable template source.
- Keep generated file changes predictable and avoid manual copy/paste setup.

Use `templates copy` only when you intentionally want a low-level file copy without Fabricator project validation.

## Prerequisites

The target project must be created by rn-fabricator or otherwise follow the [Fabricator Project Contract](fabricator-project-contract.md).

The project root must include:

```text
.fabricator/project.json
fabricator.json
src/
```

`.fabricator/project.json` tells rn-fabricator where files can be written safely. Root `fabricator.json` records template sources and applied template operations so `templates status` can explain what happened later.

For release-stable examples, use the catalog from the matching release tag:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="https://raw.githubusercontent.com/gitsametcan/rn-fabricator/v1.0.0-beta.2/templates/catalog.fabricator.json"
```

For latest development examples, use the `develop` catalog:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json"
```

For local dogfooding, point to the repository catalog:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="/Users/sametcan/Documents/GitHub/fabricator/templates/catalog.fabricator.json"
```

For local template authoring, create a repo-external playground and a writable local catalog workspace:

```bash
mkdir -p ~/Documents/rn-fabricator-playground/local-templates
cd ~/Documents/rn-fabricator-playground
export RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE="$PWD/local-templates/catalog.fabricator.json"
```

Use the remote or repository catalog for discovering existing templates. Use `RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE` for add, update, remove, and local validate operations.

## 1. Create A Compatible Project

Create a React Native CLI project with the default minimal starter:

```bash
rn-fabricator create FabricatorBabyStep \
  --output . \
  --template-source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Expected result:

- React Native CLI project files are created.
- The minimal splash and main starter is applied.
- `.fabricator/project.json` declares the Fabricator project contract.
- Root `fabricator.json` records the create operation and the starter template source.
- Standard folders such as `src/screens`, `src/components`, `src/services`, and `src/utils` exist.

Inspect project state:

```bash
rn-fabricator templates status \
  --project ./FabricatorBabyStep
```

The status output should show the create-time `minimal-splash` entry. Later `templates apply` operations are appended to the same `fabricator.json` history.

## 2. List Templates

List every template in the catalog:

```bash
rn-fabricator templates list \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

List templates by category:

```bash
rn-fabricator templates list \
  --category component \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Common categories:

| Category | Used For |
| --- | --- |
| `starter` | Create-time starter templates. |
| `screen` | Screen-level mobile views. |
| `component` | Reusable UI components. |
| `service` | API clients and external service adapters. |
| `util` | Utility helpers. |
| `layout` | Shared layout or shell components. |
| `navigation` | Navigator shells, route definitions, linking, and navigation refs. |
| `auth` | Authentication flows and helpers. |
| `config` | Configuration examples and setup helpers. |

Catalog categories describe the template type. Capture uses project folder keys from `.fabricator/project.json`, such as `screens`, `components`, `navigation`, `services`, and `utils`.

Current repository examples:

| Template | Category |
| --- | --- |
| `minimal-splash` | `starter` |
| `basic-auth` | `auth` |
| `screen/main-menu` | `screen` |
| `service/api-client` | `service` |
| `util/storage` | `util` |
| `layout/app-shell` | `layout` |
| `navigation/app-navigator` | `navigation` |
| `component/primary-button` | `component` |

## 3. Inspect Before Applying

Validate the catalog before applying templates:

```bash
rn-fabricator templates validate \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

The command reads the catalog and referenced manifests, then reports invalid schema, missing files, duplicate ids, unsupported categories, and unsafe export statements.

Inspect a template before it mutates a project:

```bash
rn-fabricator templates info component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"
```

Review:

- `Files`: source and target paths.
- `Exports`: safe barrel export statements Fabricator can add automatically.
- `Integration hints`: manual follow-up work Fabricator will not perform automatically.
- `Dependencies`: package or tool requirements the template reports.

## 4. Apply A Template

Preview the apply first:

```bash
rn-fabricator templates apply component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep \
  --dry-run
```

Apply a template to a compatible project:

```bash
rn-fabricator templates apply component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep
```

Expected result:

```text
FabricatorBabyStep/src/components/PrimaryButton.tsx
```

Preview the navigation starter template when a project needs a reusable navigation structure:

```bash
rn-fabricator templates apply navigation/app-navigator \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep \
  --dry-run
```

If the template declares a supported barrel export, Fabricator updates the matching integration point. For `component/primary-button`, this means `src/components/index.ts` receives:

```ts
export { PrimaryButton } from './PrimaryButton';
```

Apply is non-destructive by default:

- Existing files are skipped.
- Existing export statements are not duplicated.
- A successful operation is appended to `FabricatorBabyStep/fabricator.json`.
- `templates status --project ./FabricatorBabyStep` shows the applied template history.
- Use `--overwrite` only after reviewing skipped files.

```bash
rn-fabricator templates apply component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep \
  --overwrite
```

## 5. Understand Automatic Vs Manual Updates

Fabricator automatically handles:

- Project compatibility validation through `.fabricator/project.json`.
- File writes using template `targetPath`.
- `targetFolder` validation against known Fabricator project folders.
- Safe, idempotent `barrel-export` updates for declared integration points.
- Clear generated, skipped, export, and integration-note output.

Fabricator intentionally does not automate:

- Arbitrary imports into user-authored files.
- Navigation stack edits.
- Menu registry edits.
- Dependency installation.
- Environment variable or secret file creation.
- Rewriting existing app shell logic unless `--overwrite` is explicitly used.

Unsupported work is reported as integration notes so the developer can apply it deliberately.

## 6. Capture Work As A Template

Capture a known Fabricator project folder into a reusable template:

```bash
rn-fabricator templates capture profile-screens \
  --category screens \
  --from ./FabricatorBabyStep \
  --output ./captured-templates
```

Important rules:

- `--category` must match a folder key in `.fabricator/project.json`, such as `screens`, `components`, `navigation`, `services`, or `utils`.
- Without `--include`, capture copies files from that folder into a new template directory.
- With `--include`, capture copies only the selected project-relative files.
- Capture writes `fabricator-template.json` with schema v2 metadata.
- Existing output folders are not overwritten.
- Invalid capture attempts fail without publishing a partial template folder.

Expected output shape:

```text
captured-templates/
  profile-screens/
    fabricator-template.json
    src/
      screens/
        ProfileScreen.tsx
```

Capture one screen from the same project without capturing the whole `src/screens` folder:

```bash
rn-fabricator templates capture profile-screen \
  --category screens \
  --from ./FabricatorBabyStep \
  --output ./captured-templates \
  --include src/screens/ProfileScreen.tsx
```

Capture a small feature slice by selecting files from more than one Fabricator folder:

```bash
rn-fabricator templates capture profile-feature \
  --category screens \
  --from ./FabricatorBabyStep \
  --output ./captured-templates \
  --include src/screens/ProfileScreen.tsx \
  --include src/components/ProfileHeader.tsx \
  --include src/services/profileApi.ts
```

Selected files are still mapped back to their Fabricator target folders. For example, `src/screens/ProfileScreen.tsx` targets `screens`, `src/components/ProfileHeader.tsx` targets `components`, and `src/services/profileApi.ts` targets `services`.

Capture navigation files as a reusable local navigation template:

```bash
rn-fabricator templates add navigation/app-navigator \
  --category navigation \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --include src/navigation/AppNavigator.tsx \
  --include src/navigation/AuthNavigator.tsx \
  --include src/navigation/MainNavigator.tsx \
  --include src/navigation/TabNavigator.tsx \
  --include src/navigation/routes.ts \
  --include src/navigation/types.ts \
  --include src/navigation/linking.ts \
  --include src/navigation/navigationRef.ts \
  --include src/navigation/screenOptions.ts \
  --include src/navigation/index.ts \
  --dry-run
```

To capture and register the selected screen through catalog commands, add it to a catalog file:

```bash
rn-fabricator templates add profile-screen \
  --category screens \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --include src/screens/ProfileScreen.tsx \
  --dry-run
```

Drop `--dry-run` to create the template directory under `./local-templates` and register a catalog entry like this:

```bash
rn-fabricator templates add profile-screen \
  --category screens \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --include src/screens/ProfileScreen.tsx
```

```json
{
  "id": "profile-screen",
  "displayName": "Profile Screen",
  "description": "Captured profile screen.",
  "version": "0.1.0",
  "category": "screens",
  "manifest": "profile-screen/fabricator-template.json",
  "tags": ["screens"]
}
```

Validate and inspect the local catalog:

```bash
rn-fabricator templates validate \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE"

rn-fabricator templates info profile-screen \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE"
```

When the source project changes, refresh the existing local template:

```bash
rn-fabricator templates update profile-screen \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --dry-run
```

Drop `--dry-run` to apply the update:

```bash
rn-fabricator templates update profile-screen \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --include src/screens/ProfileScreen.tsx
```

Update preserves the existing manifest display name, description, version, mode, tags, dependencies, exports, and integration hints. Without `--include`, it refreshes the `files` array and template file contents from the Fabricator project folder declared by the existing manifest category. With `--include`, it refreshes only the selected files and keeps the template narrow.

Remove a template from the local catalog without deleting the template folder:

```bash
rn-fabricator templates remove profile-screen \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --dry-run
```

Drop `--dry-run` to remove only the catalog entry:

```bash
rn-fabricator templates remove profile-screen \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE"
```

To also delete the local template folder, opt in explicitly:

```bash
rn-fabricator templates remove profile-screen \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --delete-files \
  --dry-run
```

Drop `--dry-run` to apply removal. Remove does not modify projects that previously applied the template.

Then inspect and apply it like any other template:

```bash
rn-fabricator templates info profile-screen \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE"

rn-fabricator templates apply profile-screen \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --output ./AnotherFabricatorApp
```

## 7. Local Lifecycle Rules

Use this rule of thumb:

- `templates capture` creates a standalone template folder but does not register it in a catalog.
- `templates add` captures and registers a new template in a local catalog.
- `templates update` refreshes an existing local template from the source project folder.
- `--include` narrows capture, add, or update to one or more project-relative files.
- `templates remove` removes a local catalog entry and keeps files unless `--delete-files` is passed.
- `--dry-run` is the first command to run before any add, update, remove, or apply operation.

Local lifecycle commands require a filesystem catalog path. Remote GitHub raw URLs are read-only sources for list, info, validate, copy, and apply.

## 8. Dogfooding Checklist

Use a repo-external playground directory:

```bash
mkdir -p ~/Documents/rn-fabricator-playground
cd ~/Documents/rn-fabricator-playground
```

Run the reusable template workflow:

```bash
rn-fabricator create FabricatorBabyStep \
  --output . \
  --template-source "$RN_FABRICATOR_TEMPLATE_SOURCE"

rn-fabricator templates list \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"

rn-fabricator templates info component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE"

rn-fabricator templates apply component/primary-button \
  --source "$RN_FABRICATOR_TEMPLATE_SOURCE" \
  --output ./FabricatorBabyStep

rn-fabricator templates capture baby-step-screens \
  --category screens \
  --from ./FabricatorBabyStep \
  --output ./captured-templates

rn-fabricator templates add baby-step-screens \
  --category screens \
  --from ./FabricatorBabyStep \
  --source "$RN_FABRICATOR_LOCAL_TEMPLATE_SOURCE" \
  --dry-run
```

Turn feedback into issues with the command, expected behavior, actual behavior, platform, and rn-fabricator version.
