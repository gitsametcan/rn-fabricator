# Release Workflow

This document defines how rn-fabricator versions, changelog entries, release notes, and package builds are prepared.

## Versioning Policy

rn-fabricator uses semantic versioning for public releases:

- `MAJOR` changes for breaking CLI behavior or template contracts.
- `MINOR` changes for backwards-compatible commands, options, templates, and checks.
- `PATCH` changes for backwards-compatible fixes and documentation corrections that ship in a package.

Pre-release builds use a suffix such as `alpha.1`, `beta.1`, or `rc.1`.

Examples:

```text
0.5.0-alpha.1
0.5.0-rc.1
0.5.0
0.5.1
```

The repository keeps package version values in `Directory.Build.props`:

- `VersionPrefix` stores the release version, for example `0.5.0`.
- `VersionSuffix` stores the pre-release suffix, for example `alpha.1`.
- Empty `VersionSuffix` values produce stable release versions.

## Changelog Expectations

`CHANGELOG.md` is updated in the same pull request that changes release behavior or prepares a release.

Use the `Unreleased` section during normal development. Group entries under these headings when relevant:

- `Added`
- `Changed`
- `Fixed`
- `Removed`
- `Security`

Before tagging a release:

1. Rename `Unreleased` entries into a release heading such as `## 0.5.0 - 2026-06-17`.
2. Add a fresh empty `## Unreleased` section above the release heading.
3. Confirm release notes match the merged issues and pull requests.
4. Confirm package version values in `Directory.Build.props` match the release heading.

## Repeatable Release Steps

Run these steps from a clean `develop` branch before preparing a release pull request:

```bash
git switch develop
git fetch origin
dotnet restore rn-fabricator.sln
dotnet build rn-fabricator.sln --no-restore --configuration Release
dotnet test rn-fabricator.sln --no-build --configuration Release
dotnet pack src/Fabricator.Cli/Fabricator.Cli.csproj --no-build --configuration Release --output artifacts/packages
```

Verify the package can be installed as a local .NET tool:

```bash
dotnet tool install rn-fabricator --tool-path /tmp/rn-fabricator-release-check --add-source artifacts/packages --version 0.5.0-alpha.1
/tmp/rn-fabricator-release-check/rn-fabricator --help
/tmp/rn-fabricator-release-check/rn-fabricator doctor --help
/tmp/rn-fabricator-release-check/rn-fabricator create --help
/tmp/rn-fabricator-release-check/rn-fabricator templates list --help
/tmp/rn-fabricator-release-check/rn-fabricator templates copy --help
```

Inspect the package contents before attaching or publishing it:

```bash
unzip -l artifacts/packages/rn-fabricator.0.5.0-alpha.1.nupkg
```

The package must include:

- `tools/net8.0/any/rn-fabricator.dll`
- `tools/net8.0/any/Fabricator.Core.dll`
- `tools/net8.0/any/Templates/basic-auth`
- `tools/net8.0/any/ToolchainProfiles/react-native-stable.json`
- `README.md`

Catalog templates also live in the repository-level `templates/` directory and are consumed with explicit local paths or raw GitHub URLs.

## Release Pull Request

The release pull request should include:

- Version updates in `Directory.Build.props`.
- A completed release entry in `CHANGELOG.md`.
- Any final README or release checklist updates.
- Verification output summary in the pull request body.

After the release pull request is merged, create the GitHub release from the merge commit or release tag according to the release issue.
