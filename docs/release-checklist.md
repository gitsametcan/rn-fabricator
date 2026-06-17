# Release Checklist

This checklist defines the initial release readiness expectations for rn-fabricator itself and for generated React Native projects.

## rn-fabricator Release

- README is up to date.
- CHANGELOG contains the release notes.
- Tests pass locally.
- GitHub Actions CI passes.
- Version number is updated.
- Version values in `Directory.Build.props` match the release notes.
- No secrets are committed.
- License and security policy are present.
- Generated templates have been tested on a clean machine or clean workspace.
- Packaged tool can be installed from `artifacts/packages`.

## Generated React Native Project

- `.env.example` is present.
- Real `.env` files are ignored.
- `credentials.example.json` is present.
- Real credential files are ignored.
- iOS dependencies can be installed.
- Android project can be opened or built.
- Basic auth flow starts correctly.
- Release notes or next steps are printed after generation.

## Future CLI Command

The future `release-check` command can automate parts of this checklist.
