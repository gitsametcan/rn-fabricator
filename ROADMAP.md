# Roadmap

## Phase 0: Repository Foundation

- Create project documentation.
- Add open source repository files.
- Add GitHub issue and pull request templates.
- Add initial GitHub Actions workflow.
- Create initial git branches.

## Phase 1: .NET Solution Setup

- Create `Fabricator.Core`.
- Create `Fabricator.Cli`.
- Create `Fabricator.Tests`.
- Configure xUnit.
- Configure basic CI restore, build, and test jobs.

## Phase 2: doctor Command

- Implement dependency checks.
- Add readable console output.
- Add actionable remediation hints.
- Add tests for check result mapping.

## Phase 3: create Command

- Add React Native CLI project creation orchestration.
- Add project name validation.
- Add failure handling and cleanup strategy.
- Add generated project next-step output.

## Phase 4: basic-auth Template

- Add Splash, Loading, Login, and Home screens.
- Add simple auth flow.
- Add `.env.example`.
- Add `credentials.example.json`.
- Add template tests.

## Phase 5: Packaging

- Package as a .NET tool.
- Add GitHub Release workflow.
- Add installation documentation.
- Add versioning policy.

## Phase 6: Template Catalog Workflow

- Make `create` apply only a minimal splash starter.
- Add a repository-hosted Fabricator template catalog.
- Add commands to list and copy templates after project creation.
- Keep optional templates explicit and non-destructive by default.

## Later

- Add desktop UI exploration.
- Add more templates.
- Add remote template registry support.
- Add richer release readiness checks.
