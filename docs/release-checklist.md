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
- Desktop app builds for the release target runtimes when UI changes are included.
- Desktop smoke tests pass through the normal solution test command.
- Desktop release artifacts are attached or explicitly deferred in the release notes.

## Desktop UI Release

- macOS artifact is built with the intended runtime identifier.
- Windows artifact is built with the intended runtime identifier.
- Desktop app starts locally on at least one supported platform before release.
- Command bridge and UI smoke tests pass without invoking real external tools.
- Code signing, notarization, installers, and auto-update support are documented as included or intentionally deferred.

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
