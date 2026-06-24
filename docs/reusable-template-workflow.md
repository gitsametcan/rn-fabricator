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
src/
```

The template catalog source must be explicit:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json"
```

For local dogfooding, point to the repository catalog:

```bash
export RN_FABRICATOR_TEMPLATE_SOURCE="/Users/sametcan/Documents/GitHub/fabricator/templates/catalog.fabricator.json"
```

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
- Standard folders such as `src/screens`, `src/components`, `src/services`, and `src/utils` exist.

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
| `auth` | Authentication flows and helpers. |
| `config` | Configuration examples and setup helpers. |

Catalog categories describe the template type. Capture uses project folder keys from `.fabricator/project.json`, which are usually plural, such as `screens`, `components`, `services`, and `utils`.

Current repository examples:

| Template | Category |
| --- | --- |
| `minimal-splash` | `starter` |
| `basic-auth` | `auth` |
| `screen/main-menu` | `screen` |
| `service/api-client` | `service` |
| `util/storage` | `util` |
| `layout/app-shell` | `layout` |
| `component/primary-button` | `component` |

## 3. Inspect Before Applying

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

If the template declares a supported barrel export, Fabricator updates the matching integration point. For `component/primary-button`, this means `src/components/index.ts` receives:

```ts
export { PrimaryButton } from './PrimaryButton';
```

Apply is non-destructive by default:

- Existing files are skipped.
- Existing export statements are not duplicated.
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
rn-fabricator templates capture profile-screen \
  --category screens \
  --from ./FabricatorBabyStep \
  --output ./captured-templates
```

Important rules:

- `--category` must match a folder key in `.fabricator/project.json`, such as `screens`, `components`, `services`, or `utils`.
- Capture copies files from that folder into a new template directory.
- Capture writes `fabricator-template.json` with schema v2 metadata.
- Existing output folders are not overwritten.
- Invalid capture attempts fail without publishing a partial template folder.

Expected output shape:

```text
captured-templates/
  profile-screen/
    fabricator-template.json
    src/
      screens/
        ProfileScreen.tsx
```

To reuse a captured template through catalog commands, add it to a catalog file:

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

Then inspect and apply it like any other template:

```bash
rn-fabricator templates info profile-screen \
  --source ./captured-templates/catalog.fabricator.json

rn-fabricator templates apply profile-screen \
  --source ./captured-templates/catalog.fabricator.json \
  --output ./AnotherFabricatorApp
```

## 7. Dogfooding Checklist

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
```

Turn feedback into issues with the command, expected behavior, actual behavior, platform, and rn-fabricator version.
