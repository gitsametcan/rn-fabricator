# Fabricator State Contract

This document defines the user-facing project state file that rn-fabricator writes at the root of a generated React Native project:

```text
fabricator.json
```

## Goal

`fabricator.json` is the project memory for template lifecycle operations. It lets rn-fabricator show which templates were applied, where those templates came from, what files were written or skipped, and which integration steps remain manual.

The file is intended to be readable by developers and safe to commit with the generated app. It should not contain secrets, machine-specific absolute paths, or local access tokens.

## Relationship To `.fabricator/project.json`

Fabricator projects use two different metadata files:

| File | Audience | Purpose |
| --- | --- | --- |
| `fabricator.json` | User-facing project state | Tracks template sources and template lifecycle operations performed on the project. |
| `.fabricator/project.json` | Internal compatibility contract | Describes the generated project structure, folder keys, and safe integration points. |

`.fabricator/project.json` answers whether a project can safely receive a template. `fabricator.json` answers what rn-fabricator already did to that project.

Template commands may read both files:

- Compatibility checks use `.fabricator/project.json`.
- Source resolution, applied template history, and lifecycle status use `fabricator.json`.
- If `fabricator.json` is missing, commands should explain how to initialize or repair project state instead of guessing silently.

## Initial Schema

Initial schema version:

```json
{
  "schemaVersion": 1,
  "kind": "fabricator-project-state",
  "toolVersion": "1.0.0-beta.1",
  "project": {
    "name": "FabricatorDemo",
    "type": "react-native-cli",
    "createdAt": "2026-06-24T12:00:00Z"
  },
  "templateSources": [
    {
      "name": "embedded",
      "type": "embedded",
      "value": "minimal-splash",
      "isDefault": true
    },
    {
      "name": "release",
      "type": "remote",
      "value": "https://raw.githubusercontent.com/gitsametcan/rn-fabricator/v1.0.0-beta.1/templates/catalog.fabricator.json",
      "isDefault": false
    }
  ],
  "appliedTemplates": [
    {
      "id": "minimal-splash",
      "version": "0.1.0",
      "category": "starter",
      "source": {
        "name": "release",
        "type": "remote",
        "value": "https://raw.githubusercontent.com/gitsametcan/rn-fabricator/v1.0.0-beta.1/templates/catalog.fabricator.json"
      },
      "appliedAt": "2026-06-24T12:01:00Z",
      "operation": "create",
      "result": "applied",
      "files": {
        "written": [
          "App.tsx",
          "src/screens/SplashScreen.tsx",
          "src/screens/MainScreen.tsx"
        ],
        "skipped": [],
        "overwritten": []
      },
      "exports": [
        {
          "integrationPoint": "screensBarrel",
          "path": "src/screens/index.ts",
          "statement": "export { SplashScreen } from './SplashScreen';",
          "result": "added"
        }
      ],
      "integrationNotes": [
        {
          "type": "manual",
          "target": "navigation",
          "message": "Add this screen to your navigation stack if needed."
        }
      ]
    }
  ]
}
```

## Top-Level Fields

| Field | Required | Meaning |
| --- | --- | --- |
| `schemaVersion` | Yes | State schema version supported by the installed CLI. |
| `kind` | Yes | Must be `fabricator-project-state`. |
| `toolVersion` | Yes | rn-fabricator version that last wrote the state file. |
| `project` | Yes | Basic project metadata captured during create or initialization. |
| `templateSources` | Yes | Named local or remote catalogs available to template commands. |
| `appliedTemplates` | Yes | Ordered history of template operations applied to this project. |

Commands that update `fabricator.json` should preserve unknown top-level fields so future extensions do not destroy user or newer-tool metadata.

## Project Metadata

`project` stores stable project information:

| Field | Required | Meaning |
| --- | --- | --- |
| `name` | Yes | React Native project name. |
| `type` | Yes | Project type, initially `react-native-cli`. |
| `createdAt` | Yes | UTC timestamp in ISO 8601 format. |

Project metadata should not store local machine paths. Commands can resolve paths from the current project root.

## Template Sources

`templateSources` stores named catalogs that commands can use when `--source` is not provided.

Each source uses this shape:

```json
{
  "name": "local",
  "type": "local",
  "value": "./templates/catalog.fabricator.json",
  "isDefault": true
}
```

Source fields:

| Field | Required | Meaning |
| --- | --- | --- |
| `name` | Yes | Stable source name used in status and operation history. |
| `type` | Yes | `local` or `remote`. |
| `value` | Yes | Embedded template id, local catalog path, or remote catalog URL. |
| `isDefault` | No | Marks the preferred source when multiple sources exist. |

Supported source types:

| Type | Meaning |
| --- | --- |
| `embedded` | Template content came from the installed CLI rather than an external catalog. |
| `local` | Template content came from a local catalog file. |
| `remote` | Template content came from a remote catalog URL. |

Local source paths should be relative to the project root whenever possible. Absolute paths are discouraged because committed project state should work on another developer machine.

## Applied Templates

`appliedTemplates` is an ordered operation history. Commands should append a new entry for each successful template operation instead of rewriting older entries.

Each entry uses this shape:

```json
{
  "id": "component/primary-button",
  "version": "0.1.0",
  "category": "component",
  "source": {
    "name": "local",
    "type": "local",
    "value": "./templates/catalog.fabricator.json"
  },
  "appliedAt": "2026-06-24T12:10:00Z",
  "operation": "apply",
  "result": "applied",
  "files": {
    "written": ["src/components/PrimaryButton.tsx"],
    "skipped": [],
    "overwritten": []
  },
  "exports": [
    {
      "integrationPoint": "componentsBarrel",
      "path": "src/components/index.ts",
      "statement": "export { PrimaryButton } from './PrimaryButton';",
      "result": "added"
    }
  ],
  "integrationNotes": []
}
```

Operation fields:

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | Yes | Template id from the catalog or manifest. |
| `version` | Yes | Template version that was applied. |
| `category` | Yes | Template category, such as `starter`, `screen`, `component`, `service`, or `util`. |
| `source` | Yes | Source catalog used for this operation. |
| `appliedAt` | Yes | UTC timestamp in ISO 8601 format. |
| `operation` | Yes | Template lifecycle operation, such as `create`, `apply`, `add`, `update`, or `remove`. |
| `result` | Yes | Operation result, such as `applied`, `updated`, `removed`, or `skipped`. |
| `files` | Yes | Files written, skipped, or overwritten by the operation. |
| `exports` | Yes | Barrel export updates attempted by the operation. |
| `integrationNotes` | Yes | Manual or unsupported integration work reported by the template. |

## File Results

Template operations should report file changes in relative paths from the project root:

```json
{
  "written": ["src/services/apiClient.ts"],
  "skipped": ["src/services/index.ts"],
  "overwritten": []
}
```

`written` contains files created by the operation. `skipped` contains files that already existed or could not be changed safely. `overwritten` contains files intentionally replaced when the user passes an explicit overwrite option.

Commands must not record paths that escape the project root.

## Export Results

Export updates use the integration point keys from `.fabricator/project.json`:

```json
{
  "integrationPoint": "servicesBarrel",
  "path": "src/services/index.ts",
  "statement": "export { apiClient } from './apiClient';",
  "result": "already-exists"
}
```

Supported results:

| Result | Meaning |
| --- | --- |
| `added` | The export statement was appended. |
| `already-exists` | The export statement was already present. |
| `skipped` | The export could not be changed safely. |
| `unsupported` | The requested integration point type is not automated. |

Commands should record skipped or unsupported export work so `templates status` can surface it later.

## Integration Notes

Integration notes preserve template guidance that rn-fabricator cannot safely automate yet:

```json
{
  "type": "manual",
  "target": "navigation",
  "message": "Add MainMenuScreen to your navigation stack if needed."
}
```

Notes should be short, actionable, and safe to print in CLI output.

## Source Resolution

Template commands should use this source resolution order:

1. Explicit `--source`.
2. `RN_FABRICATOR_TEMPLATE_SOURCE`.
3. Default source from project root `fabricator.json`.
4. Conventional local source at `./templates/catalog.fabricator.json`.

When multiple `templateSources` are available in `fabricator.json`, commands should prefer the source marked with `isDefault: true`. If no default exists, they may use the first source and print the selected source clearly.

## Compatibility Rules

A state file is valid when:

- `fabricator.json` exists at the project root.
- `schemaVersion` is supported by the installed CLI.
- `kind` is `fabricator-project-state`.
- `project.type` is `react-native-cli`.
- `templateSources` is an array.
- `appliedTemplates` is an array.
- Source types are `embedded`, `local`, or `remote`.
- Local source values are relative paths or safe paths inside the intended workspace.
- File and export paths are relative and cannot escape the project root.

If any rule fails, mutating commands should stop before writing project files and explain the recovery path.

## Backwards Compatibility

Future schema versions should follow these rules:

- Add optional fields before changing existing field meaning.
- Preserve unknown fields when reading and writing state.
- Provide a clear migration path when a new CLI can upgrade an older state file.
- Refuse to mutate a state file with a newer unsupported schema version.
- Keep committed `fabricator.json` files free of secrets and local machine assumptions.

## Command Responsibilities

`rn-fabricator create` should initialize `fabricator.json` after the React Native CLI project is created and the starter template is applied.

`templates apply` should append an operation entry after a template is applied or safely skipped.

`templates status` should read `fabricator.json` and summarize applied templates, sources, versions, and unresolved integration notes.

`templates add`, `templates update`, and `templates remove` should update local catalogs and record project-level state only when they operate against a project root.
