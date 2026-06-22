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
- A minimal splash screen is resolved from the Fabricator template catalog and applied.
- No auth flow is applied automatically.
- The generated app has a predictable `src` structure.
- The generated app can declare Fabricator compatibility with `.fabricator/project.json`.

List available templates:

```bash
rn-fabricator templates list --source https://raw.githubusercontent.com/gitsametcan/rn-fabricator/develop/templates/catalog.fabricator.json
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

## Initial Templates

| Template | Type | Status | Purpose |
| --- | --- | --- | --- |
| `minimal-splash` | create default | Planned catalog entry | Minimal app shell with a splash screen and base folders. |
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
