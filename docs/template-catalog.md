# Template Catalog

This document defines the planned rn-fabricator template workflow.

## Goal

`create` should stay small and predictable. It should create a React Native CLI app and apply only the minimal starter experience needed to prove the generated app works.

Additional starter features should live in a Fabricator template catalog and be applied explicitly by the user after project creation.

Templates are not bundled into the installed CLI package by default. The CLI reads a catalog URL or a local catalog file, then uses that catalog to list, inspect, copy, or apply available templates.

Catalog templates are applied to projects that follow the [Fabricator Project Contract](fabricator-project-contract.md). The contract defines the target folder layout, project manifest, and safe integration points that future apply/capture commands can rely on.

## Planned User Flow

Create a new app with the default minimal starter:

```bash
rn-fabricator create FabricatorBabyStep --output . --template-source ./templates/catalog.fabricator.json
```

Expected result:

- React Native CLI project is created.
- A minimal splash and main screen starter is resolved from the Fabricator template catalog and applied.
- No auth flow is applied automatically.
- The generated app has a predictable `src` structure.
- The generated app can declare Fabricator compatibility with `.fabricator/project.json`.

List available templates:

```bash
rn-fabricator templates list --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
rn-fabricator templates list --category auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

Inspect a template before copying or applying it:

```bash
rn-fabricator templates info basic-auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

Apply a template into a compatible Fabricator project:

```bash
rn-fabricator templates apply basic-auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
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

Expected copy behavior:

- Copy remains a lower-level escape hatch.
- It writes template source paths directly to the output directory.
- It does not require a Fabricator project manifest.

Capture a reusable template from a compatible Fabricator project folder:

```bash
rn-fabricator templates capture profile-screen --category screens --from ./FabricatorBabyStep --output ./templates
```

Expected capture behavior:

- The source project must follow the Fabricator project contract.
- `--category` must match a folder key in `.fabricator/project.json`, such as `screens`, `components`, `services`, or `utils`.
- Files under that folder are copied into a new template folder.
- The generated `fabricator-template.json` includes schema v2 metadata, category, tags, `targetPath`, and `targetFolder` mappings.
- Existing template output folders are not overwritten.
- Invalid capture attempts fail before publishing a partial template folder.

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
| `navigation` | Navigation/menu-related files. |
| `auth` | Authentication flows and helpers. |
| `config` | Configuration examples or setup helpers. |

### Apply Rules

Schema v2 separates source paths from target intent:

- `path`: file location inside the template folder.
- `targetPath`: destination path relative to the target project.
- `targetFolder`: optional key from `.fabricator/project.json` such as `screens`, `services`, or `utils`.
- `exports`: idempotent barrel export statements that `templates apply` may add safely.
- `dependencies`: package or tool requirements the CLI can report before applying.
- `integrationHints`: manual or future automated follow-up instructions.

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

Planned command family:

```text
rn-fabricator templates list --source <catalog-url-or-path>
rn-fabricator templates list --category <category> --source <catalog-url-or-path>
rn-fabricator templates info <template> --source <catalog-url-or-path>
rn-fabricator templates apply <template> --source <catalog-url-or-path>
rn-fabricator templates capture <template> --category <category> --from <project-path>
rn-fabricator templates copy <template> --source <catalog-url-or-path>
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

For local dogfooding, `--source` can point to:

```text
/Users/sametcan/Documents/GitHub/fabricator/templates/catalog.fabricator.json
```

For GitHub-hosted usage, `--source` can point to the raw catalog file:

```text
https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

The CLI may later add a default catalog URL, but the first implementation should keep the source explicit so behavior is easy to inspect and test.
