# Contributing

Thanks for your interest in contributing to rn-fabricator.

The project is in its initial setup phase. Contributions should stay aligned with the documented MVP until the first usable CLI release is complete.

## Development Principles

- Keep the CLI predictable and easy to understand.
- Prefer tested core logic over behavior hidden in command handlers.
- Avoid generating real secrets or machine-specific files.
- Keep templates readable for React Native developers.
- Document user-facing behavior.

## Expected Workflow

1. Open an issue for bugs, features, or larger changes.
2. Create a feature branch from the active development branch.
3. Add or update tests for behavior changes.
4. Update documentation when commands, templates, or setup steps change.
5. Open a pull request using the pull request template.

## Branch Naming

Recommended branch prefixes:

- `feature/`
- `fix/`
- `docs/`
- `chore/`

## Commit Style

Use short, direct commit messages.

Examples:

```text
docs: add product brief
feature: add doctor command skeleton
fix: handle missing npm path
```

## Local Checks

After the .NET solution is created, run:

```bash
dotnet restore
dotnet build
dotnet test
```
