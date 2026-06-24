# Fabricator Project Contract

This document defines the project contract for React Native apps that can safely receive rn-fabricator templates.

## Goal

Fabricator templates should only be applied to projects with a predictable structure. A generated project declares that structure with a project manifest:

```text
.fabricator/project.json
```

The manifest lets future commands validate compatibility before copying files, updating exports, or applying integration rules.

## Manifest Shape

Initial schema version:

```json
{
  "schemaVersion": 1,
  "kind": "fabricator-react-native-project",
  "fabricatorVersion": "0.9.0",
  "projectType": "react-native-cli",
  "sourceRoot": "src",
  "folders": [
    {
      "key": "app",
      "path": "src/app",
      "purpose": "Application composition and app-level wiring."
    },
    {
      "key": "components",
      "path": "src/components",
      "purpose": "Reusable UI components."
    },
    {
      "key": "config",
      "path": "src/config",
      "purpose": "Application configuration helpers and constants."
    },
    {
      "key": "constants",
      "path": "src/constants",
      "purpose": "Shared static values."
    },
    {
      "key": "hooks",
      "path": "src/hooks",
      "purpose": "Reusable React hooks."
    },
    {
      "key": "screens",
      "path": "src/screens",
      "purpose": "Screen-level mobile views."
    },
    {
      "key": "services",
      "path": "src/services",
      "purpose": "API clients and external service adapters."
    },
    {
      "key": "storage",
      "path": "src/storage",
      "purpose": "Local persistence helpers."
    },
    {
      "key": "theme",
      "path": "src/theme",
      "purpose": "Theme tokens and styling helpers."
    },
    {
      "key": "types",
      "path": "src/types",
      "purpose": "Shared TypeScript types."
    },
    {
      "key": "utils",
      "path": "src/utils",
      "purpose": "Reusable utility functions."
    }
  ],
  "integrationPoints": [
    {
      "key": "screensBarrel",
      "path": "src/screens/index.ts",
      "type": "barrel-export",
      "description": "Screen exports managed by Fabricator."
    },
    {
      "key": "componentsBarrel",
      "path": "src/components/index.ts",
      "type": "barrel-export",
      "description": "Component exports managed by Fabricator."
    },
    {
      "key": "servicesBarrel",
      "path": "src/services/index.ts",
      "type": "barrel-export",
      "description": "Service exports managed by Fabricator."
    }
  ]
}
```

## Compatibility Rules

A project is compatible when:

- `.fabricator/project.json` exists at the project root.
- `schemaVersion` is supported by the installed rn-fabricator version.
- `kind` is `fabricator-react-native-project`.
- `projectType` is `react-native-cli`.
- `sourceRoot` exists.
- Every declared folder path exists.
- Every declared integration point path exists when its type requires an existing file.
- Manifest paths are relative and cannot escape the project root.

If any rule fails, template commands must stop before writing files and explain what is missing.

## Compatibility Validator

Core validation is provided by `FabricatorProjectCompatibilityValidator`.

The validator is read-only. It checks the target project before future template apply/capture commands mutate files:

- The project directory exists.
- `.fabricator/project.json` exists and can be parsed.
- `schemaVersion`, `kind`, and `projectType` match supported values.
- `sourceRoot` exists and stays inside the project root.
- Every declared folder path exists and stays inside the project root.
- Every declared integration point path stays inside the project root.
- `barrel-export` integration points point to existing files.
- Unsupported integration point types are reported as compatibility errors until the tool knows how to handle them.

The existing `templates copy` command remains a low-level copy operation. Smart template application uses this validator before writing files.

## Supported Integration Point Types

Initial supported type:

| Type | Meaning |
| --- | --- |
| `barrel-export` | Fabricator may safely append idempotent export lines to this file. |

Unsupported integration point types must be reported as manual follow-up work instead of being silently applied.

`templates apply` currently automates only `barrel-export` updates. A template export is applied only when the statement is a single-line `export ... from ...;` barrel export and the target integration point exists in `.fabricator/project.json`.

Registry-style work, such as menu or navigation entries, is intentionally not automated by this integration type. Templates should expose those needs as integration hints until a dedicated registry integration point is added to the project contract.

## Relationship To Templates

Template manifests describe what a template wants to add. The project manifest describes where the target project allows template work.

Apply behavior uses both inputs:

- Validate the project contract first.
- Resolve template target paths against manifest folders.
- Apply files non-destructively by default.
- Apply only supported integration updates automatically.
- Print manual integration hints for anything outside the supported contract.

## Create Command Responsibility

`rn-fabricator create` should generate this manifest for new projects after the React Native CLI project is created and the starter files are applied.

The first compatible generated app should remain small: Splash screen, Main screen, the standard folder layout, and `.fabricator/project.json`.
