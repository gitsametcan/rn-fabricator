# Template Catalog

This document defines the planned rn-fabricator template workflow.

## Goal

`create` should stay small and predictable. It should create a React Native CLI app and apply only the minimal starter experience needed to prove the generated app works.

Additional starter features should live in a Fabricator template catalog and be copied explicitly by the user after project creation.

Templates are not bundled into the installed CLI package by default. The CLI should read a catalog URL or a local catalog file, then use that catalog to list and copy available templates.

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

Copy a template into the current project:

```bash
rn-fabricator templates copy basic-auth --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
```

Expected copy behavior:

- Copy is explicit; `create` does not apply optional templates.
- Existing files are not overwritten unless the user passes an explicit overwrite option.
- The command prints generated, skipped, and next-step output.

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
- `exports`: idempotent barrel export statements that future apply commands may add safely.
- `dependencies`: package or tool requirements the CLI can report before applying.
- `integrationHints`: manual or future automated follow-up instructions.

Current copy behavior still reads `path` and writes files to the same relative location. Future apply behavior should prefer `targetPath` and validate `targetFolder` against the Fabricator project contract.

## Initial Templates

| Template | Type | Status | Purpose |
| --- | --- | --- | --- |
| `minimal-splash` | create default | Planned catalog entry | Minimal app shell with splash and main screens plus base folders. |
| `basic-auth` | optional copy | Planned catalog entry | Splash, loading, login, home, auth provider, env examples, and credential example. |

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
- Future template apply commands should validate `.fabricator/project.json` before mutating the project.

## Command Design Notes

Planned command family:

```text
rn-fabricator templates list --source <catalog-url-or-path>
rn-fabricator templates list --category <category> --source <catalog-url-or-path>
rn-fabricator templates info <template> --source <catalog-url-or-path>
rn-fabricator templates copy <template> --source <catalog-url-or-path>
```

Future options:

```text
--output <path>
--overwrite
--dry-run
--source <catalog-url-or-path>
```

The first implementation should keep copy behavior conservative and transparent.

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
