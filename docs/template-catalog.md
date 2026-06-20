# Template Catalog

This document defines the planned rn-fabricator template workflow.

## Goal

`create` should stay small and predictable. It should create a React Native CLI app and apply only the minimal starter experience needed to prove the generated app works.

Additional starter features should live in the template catalog and be copied explicitly by the user after project creation.

## Planned User Flow

Create a new app with the default minimal starter:

```bash
rn-fabricator create FabricatorBabyStep --output .
```

Expected result:

- React Native CLI project is created.
- A minimal splash screen is applied.
- No auth flow is applied automatically.
- The generated app has a predictable `src` structure.

List available templates:

```bash
rn-fabricator templates list
```

Copy a template into the current project:

```bash
rn-fabricator templates copy basic-auth
```

Expected copy behavior:

- Copy is explicit; `create` does not apply optional templates.
- Existing files are not overwritten unless the user passes an explicit overwrite option.
- The command prints generated, skipped, and next-step output.

## Template Storage

Packaged template assets live in:

```text
src/Fabricator.Core/TemplateAssets/
```

Each template directory should contain:

- `template.json`
- source files to copy
- optional documentation such as `README.md` or `CONFIGURATION.md`

## Initial Templates

| Template | Type | Status | Purpose |
| --- | --- | --- | --- |
| `minimal-splash` | create default | Planned | Minimal app shell with a splash screen and base folders. |
| `basic-auth` | optional copy | Existing assets, not wired | Splash, loading, login, home, auth provider, env examples, and credential example. |

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

## Command Design Notes

Planned command family:

```text
rn-fabricator templates list
rn-fabricator templates copy <template>
```

Future options:

```text
--output <path>
--overwrite
--dry-run
```

The first implementation should keep copy behavior conservative and transparent.
