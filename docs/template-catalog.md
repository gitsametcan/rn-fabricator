# Template Catalog

This document defines the rn-fabricator template catalog model.

## Goal

`create` should stay small and predictable. It should create a React Native CLI app and apply only the minimal starter experience needed to prove the generated app works.

Additional starter features should live in a Fabricator template catalog and be applied explicitly by the user after project creation.

Templates are not bundled into the installed CLI package by default. The CLI reads a catalog URL or a local catalog file, then uses that catalog to list, inspect, copy, or apply available templates.

Catalog templates are applied to projects that follow the [Fabricator Project Contract](fabricator-project-contract.md). The contract defines the target folder layout, project manifest, and safe integration points that apply/capture commands rely on.

Template lifecycle commands also use the root [Fabricator State Contract](fabricator-state-contract.md) to track configured template sources and applied template history.

For developer-facing usage steps, see [Reusable Template Workflow](reusable-template-workflow.md).

## User Flow

Create a new app with the default minimal starter:

```bash
rn-fabricator create FabricatorBabyStep --output . --template-source ./templates/catalog.fabricator.json
```

Expected result:

- React Native CLI project is created.
- A minimal splash and main screen starter is resolved from the Fabricator template catalog and applied.
- A dependency-free navigation starter structure is available under `src/navigation`.
- No auth flow is applied automatically.
- The generated app has a predictable `src` structure.
- The generated app can declare Fabricator compatibility with `.fabricator/project.json`.

List available templates:

```bash
rn-fabricator templates list
rn-fabricator templates list --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
rn-fabricator templates list --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/v1.0.0-beta.2/templates/catalog.fabricator.json
rn-fabricator templates list --category auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
rn-fabricator templates list --category navigation --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

Inspect a template before copying or applying it:

```bash
rn-fabricator templates info basic-auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
rn-fabricator templates info navigation/app-navigator --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

Validate a catalog before applying or publishing templates:

```bash
rn-fabricator templates validate
rn-fabricator templates validate --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

Apply a template into a compatible Fabricator project:

```bash
rn-fabricator templates apply basic-auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
rn-fabricator templates apply basic-auth --source ./templates/catalog.fabricator.json --output ./FabricatorBabyStep --dry-run
rn-fabricator templates apply navigation/app-navigator --source ./templates/catalog.fabricator.json --output ./FabricatorBabyStep --dry-run
```

Show template history for a compatible Fabricator project:

```bash
rn-fabricator templates status --project ./MyApp
```

Copy a template into the current directory without Fabricator project validation:

```bash
rn-fabricator templates copy basic-auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

Expected apply behavior:

- Apply is explicit; `create` does not apply optional templates.
- The target directory must follow the Fabricator project contract and include `.fabricator/project.json`.
- Template files are written to `targetPath` when the manifest defines it.
- `targetFolder` is validated against the Fabricator project contract before writing.
- Existing files are not overwritten unless the user passes an explicit overwrite option.
- The command prints generated and skipped file output.
- Successful apply operations are appended to root `fabricator.json` so later status and lifecycle commands can inspect template history.
- `templates status` reads root `fabricator.json`, prints applied template history, and compares versions against recorded local catalog sources when those files are available.
- `templates apply --dry-run` prints planned file writes, export updates, skips, and integration notes without writing project files, exports, or `fabricator.json`.

Template command output should stay screenshot-friendly and actionable. Successful commands print a concise `Summary:` line, and commands that leave a follow-up action for the developer print a `Next:` line. Failures include the failing template id, catalog source, project path, or file path when that information is available.

## Template Source Resolution

Template commands can read a source explicitly or resolve one from the local workspace.

Source precedence:

1. Explicit `--source`.
2. `RN_FABRICATOR_TEMPLATE_SOURCE`.
3. Default local or remote catalog source from root `fabricator.json`.
4. Conventional local catalog at `./templates/catalog.fabricator.json`.

`fabricator.json` entries with source type `embedded` are skipped for reusable template commands because they describe starter content that came from the installed CLI, not an external catalog.

Relative source paths from `fabricator.json` are resolved from the directory that contains that state file. Relative environment variable sources are resolved from the current working directory. Explicit relative `--source` values keep the existing command-line behavior.

Recommended local authoring workspace:

```text
rn-fabricator-playground/
  FabricatorBabyStep/
    .fabricator/project.json
    fabricator.json
    src/
  local-templates/
    catalog.fabricator.json
    profile-screen/
      fabricator-template.json
```

Use the release-tagged raw catalog for reproducible read-only examples. Use a local filesystem catalog, such as `./local-templates/catalog.fabricator.json`, when running `templates add`, `templates update`, or `templates remove`.

Expected copy behavior:

- Copy remains a lower-level escape hatch.
- It writes template source paths directly to the output directory.
- It does not require a Fabricator project manifest.

Expected validate behavior:

- Resolve the catalog source with the same source precedence as list, info, copy, and apply.
- Read the catalog and every referenced manifest.
- Detect duplicate ids, missing manifests, missing template files, unsupported schema versions, unsupported categories, and unsafe export statements.
- Print a clear pass/fail summary and return a non-zero exit code when issues are found.

Capture a reusable template from a compatible Fabricator project folder:

```bash
rn-fabricator templates capture profile-screen --category screens --from ./FabricatorBabyStep --output ./templates
```

Capture and register a reusable template in a local catalog:

```bash
rn-fabricator templates add profile-screen --category screens --from ./FabricatorBabyStep --source ./templates/catalog.fabricator.json
rn-fabricator templates add profile-screen --category screens --from ./FabricatorBabyStep --source ./templates/catalog.fabricator.json --dry-run
```

Refresh an existing local template from the current project files:

```bash
rn-fabricator templates update profile-screen --from ./FabricatorBabyStep --source ./templates/catalog.fabricator.json
rn-fabricator templates update profile-screen --from ./FabricatorBabyStep --source ./templates/catalog.fabricator.json --dry-run
```

Remove a template entry from a local catalog:

```bash
rn-fabricator templates remove profile-screen --source ./templates/catalog.fabricator.json
rn-fabricator templates remove profile-screen --source ./templates/catalog.fabricator.json --dry-run
```

Delete local template files only when explicitly requested:

```bash
rn-fabricator templates remove profile-screen --source ./templates/catalog.fabricator.json --delete-files
rn-fabricator templates remove profile-screen --source ./templates/catalog.fabricator.json --delete-files --dry-run
```

Expected capture behavior:

- The source project must follow the Fabricator project contract.
- `--category` must match a folder key in `.fabricator/project.json`, such as `screens`, `components`, `navigation`, `services`, or `utils`.
- Files under that folder are copied into a new template folder.
- The generated `fabricator-template.json` includes schema v2 metadata, category, tags, `targetPath`, and `targetFolder` mappings.
- Existing template output folders are not overwritten.
- Invalid capture attempts fail before publishing a partial template folder.

Expected add behavior:

- `templates add` performs the same safe capture into the catalog directory.
- The local `catalog.fabricator.json` file is created when missing.
- Existing catalog metadata is preserved and the `templates` array is sorted by template id.
- Duplicate template ids are rejected; use the update flow for existing templates.
- Remote catalog URLs are read-only and cannot receive new template entries.
- `--dry-run` previews captured files and the catalog entry without writing the template folder or catalog file.

Expected update behavior:

- `templates update` requires the template id to already exist in the selected local catalog.
- The existing manifest category decides which Fabricator project folder is captured.
- The template files and manifest `files` array are refreshed from the source project.
- Manifest metadata is preserved where possible: display name, description, version, mode, tags, dependencies, exports, and integration hints.
- The command prints added, changed, removed, and unchanged file counts.
- Remote catalog URLs are read-only and cannot be updated.
- `--dry-run` previews file and catalog changes without updating the template folder or catalog file.

Expected remove behavior:

- `templates remove` requires the template id to exist in the selected local catalog.
- By default, only the catalog entry is removed; the template folder is kept.
- `--delete-files` is required before the CLI deletes the local template folder.
- File deletion is allowed only when the manifest path resolves to a template folder under the catalog directory.
- Target projects that previously applied the template are not modified.
- Remote catalog URLs are read-only and cannot be updated.
- `--dry-run` previews catalog removal and optional file deletion without changing the catalog or deleting files.

## Local Template Lifecycle Workflow

1. Create or open a Fabricator-compatible project.
2. Create a local template workspace outside the project root.
3. Capture the first version with `templates add --dry-run`.
4. Run `templates add` without `--dry-run` after reviewing output.
5. Run `templates validate --source ./local-templates/catalog.fabricator.json`.
6. Run `templates info <template> --source ./local-templates/catalog.fabricator.json`.
7. Apply the template to a second Fabricator project with `templates apply --dry-run`, then without dry-run.
8. When the source project changes, refresh the template with `templates update --dry-run`, then without dry-run.
9. Remove deprecated templates with `templates remove --dry-run`; pass `--delete-files` only when deleting the local template folder is intended.

`templates add`, `templates update`, and `templates remove` modify local catalog files and local template folders. They do not modify target projects that previously applied a template.

`templates apply` is the project-mutating command. It writes files into the target project, appends safe barrel exports, and records successful operations in root `fabricator.json`.

## Template Source

The first catalog source lives in the repository root:

```text
templates/
  catalog.fabricator.json
  minimal-splash/
    fabricator-template.json
  basic-auth/
    fabricator-template.json
```

`catalog.fabricator.json` is the public entry point. The CLI can list templates from this file without downloading or bundling every template in the .NET tool package.

Each template directory should contain a Fabricator-specific manifest:

- `fabricator-template.json`
- source file definitions or remote file references
- optional documentation such as `README.md` or `CONFIGURATION.md`

The manifest name intentionally includes `fabricator` so users can recognize that the file is meant for rn-fabricator.

## Template Schema v2

Schema v2 keeps the existing v1 fields and adds metadata needed for reusable template apply and capture workflows.

Catalog entries may include a top-level category so large catalogs can be filtered without downloading every manifest:

```json
{
  "id": "service/api-client",
  "displayName": "API Client",
  "description": "Fetch-based API client starter.",
  "version": "0.1.0",
  "category": "service",
  "manifest": "service/api-client/fabricator-template.json",
  "tags": ["service", "api", "network"]
}
```

Template manifests use this shape:

```json
{
  "schemaVersion": 2,
  "kind": "fabricator-template",
  "id": "screen/main-menu",
  "displayName": "Main Menu Screen",
  "description": "Reusable main menu screen.",
  "version": "0.1.0",
  "mode": "apply",
  "category": "screen",
  "tags": ["screen", "menu"],
  "files": [
    {
      "path": "src/screens/MainMenuScreen.tsx",
      "type": "file",
      "targetFolder": "screens",
      "targetPath": "src/screens/MainMenuScreen.tsx",
      "description": "Main menu screen component."
    }
  ],
  "dependencies": [
    {
      "type": "npm",
      "name": "@react-navigation/native",
      "version": "^7.0.0",
      "reason": "Required when the template is wired into navigation."
    }
  ],
  "exports": [
    {
      "integrationPoint": "screensBarrel",
      "statement": "export { MainMenuScreen } from './MainMenuScreen';",
      "source": "src/screens/MainMenuScreen.tsx"
    }
  ],
  "integrationHints": [
    {
      "type": "manual",
      "target": "navigation",
      "message": "Add MainMenuScreen to your navigation stack if needed."
    }
  ]
}
```

### Categories

Initial categories:

| Category | Purpose |
| --- | --- |
| `starter` | Create-time starter templates. |
| `screen` | Screen-level mobile views. |
| `component` | Reusable UI components. |
| `service` | API clients and external service adapters. |
| `util` | Reusable utility functions. |
| `integration` | Third-party integration setup. |
| `layout` | Shared shell, header, footer, or structural UI. |
| `navigation` | Navigator shells, route definitions, linking, navigation refs, and screen options. |
| `auth` | Authentication flows and helpers. |
| `config` | Configuration examples or setup helpers. |

### Apply Rules

Schema v2 separates source paths from target intent:

- `path`: file location inside the template folder.
- `targetPath`: destination path relative to the target project.
- `targetFolder`: optional key from `.fabricator/project.json` such as `screens`, `services`, or `utils`.
- `exports`: idempotent barrel export statements that `templates apply` may add safely.
- `dependencies`: package or tool requirements the CLI can report before applying.
- `integrationHints`: manual or later automated follow-up instructions.

`templates apply` prefers `targetPath`, validates `targetFolder` against the Fabricator project contract, and appends supported barrel export statements idempotently. It skips export statements that already exist and reports unsupported integration work instead of editing user-authored files.

Registry-style integrations, such as menu or navigation entries, should be represented as integration hints until the project contract defines a dedicated safe integration point type. The CLI can then report them clearly without guessing where imports should be inserted.

`templates copy` still reads `path` and writes files to the same relative location.

### Capture Rules

`templates capture` turns a known Fabricator project folder into a reusable template source. The command intentionally captures only folders declared by the project contract; it does not scan arbitrary project paths or infer imports from user-authored files.

Captured templates use the same relative `path` and `targetPath` so the generated template can be inspected, listed in a catalog, and later applied back to compatible projects. Export and registry metadata can be added manually or by later capture improvements.

## Repository Templates

| Template | Category | Status | Purpose |
| --- | --- | --- | --- |
| `minimal-splash` | `starter` | Available | Minimal app shell with splash and main screens plus base folders. |
| `basic-auth` | `auth` | Available | Splash, loading, login, home, auth provider, env examples, and credential example. |
| `screen/main-menu` | `screen` | Available | Reusable main menu screen with simple action rows and a screen barrel export. |
| `service/api-client` | `service` | Available | Fetch-based API client helper with a services barrel export. |
| `util/storage` | `util` | Available | Replaceable JSON storage helpers for local persistence adapters. |
| `layout/app-shell` | `layout` | Available | Header/content/footer application shell layout. |
| `component/primary-button` | `component` | Available | Reusable React Native primary button with a components barrel export. |

## Generated Folder Structure

The minimal starter should create a practical React Native source layout:

```text
src/
  app/
  components/
  config/
  constants/
  hooks/
  screens/
  services/
  storage/
  theme/
  types/
  utils/
```

Notes:

- Use `screens` for mobile views instead of web-style `pages`.
- Keep empty folders trackable with a small `index.ts` or README only when needed.
- Avoid adding full app architecture before the template needs it.
- Template apply commands validate `.fabricator/project.json` before mutating the project.

## Command Design Notes

Command family:

```text
rn-fabricator templates list [--source <catalog-url-or-path>]
rn-fabricator templates list --category <category> [--source <catalog-url-or-path>]
rn-fabricator templates info <template> [--source <catalog-url-or-path>]
rn-fabricator templates validate [--source <catalog-url-or-path>]
rn-fabricator templates apply <template> [--source <catalog-url-or-path>]
rn-fabricator templates capture <template> --category <category> --from <project-path>
rn-fabricator templates copy <template> [--source <catalog-url-or-path>]
```

Options:

```text
--output <path>
--overwrite
--source <catalog-url-or-path>
--category <category>
--from <project-path>
```

Copy and apply behavior should remain conservative and transparent. Existing files are skipped unless `--overwrite` is provided.

## Source Defaults

For local dogfooding, `--source` can point to a catalog explicitly, or commands can resolve the conventional local catalog automatically when it exists:

```text
/Users/sametcan/Documents/GitHub/fabricator/templates/catalog.fabricator.json
```

For GitHub-hosted usage, `--source` can point to the raw catalog file:

```text
https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

The CLI may later add a default catalog URL, but the first implementation should keep the source explicit so behavior is easy to inspect and test.
