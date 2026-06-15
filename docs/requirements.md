# Requirements

## MVP Commands

### doctor

The `doctor` command checks whether the required React Native CLI development tools are installed and available.

Initial checks:

- Node.js
- npm
- Git
- Watchman
- Xcode
- CocoaPods
- Java
- Android SDK

Expected behavior:

- Print a readable status for each dependency.
- Mark each dependency as passed, failed, or warning.
- Include a short remediation hint for failed checks.
- Return a non-zero exit code when required dependencies fail.

### create

The `create` command creates a new React Native CLI project and applies rn-fabricator conventions.

Expected behavior:

- Accept a project name.
- Invoke React Native CLI project creation.
- Apply the selected template.
- Generate example environment files.
- Print next steps after successful creation.

Example:

```bash
rn-fabricator create MyApp --template basic-auth
```

### template: basic-auth

The `basic-auth` template adds a simple authentication starter flow.

Initial template contents:

- Splash screen
- Loading screen
- Login screen
- Home screen
- Simple auth flow wiring
- `.env.example`
- `credentials.example.json`

## Functional Requirements

- The CLI must provide clear help text for every command.
- Commands must validate user input before running long operations.
- Generated files must avoid committing real secrets.
- Templates must be versioned with the CLI.
- Failures must be actionable and readable.

## Technical Requirements

- Build with .NET 8.
- Use System.CommandLine for CLI parsing where practical.
- Put reusable logic in `Fabricator.Core`.
- Keep CLI input/output concerns in `Fabricator.Cli`.
- Use xUnit for automated tests.
- Run restore, build, and tests in GitHub Actions once the solution is added.

## Out of Scope for MVP

- Desktop UI.
- Expo project generation.
- App Store or Google Play publishing automation.
- Multiple template marketplaces.
- Remote template downloads.
- Full project migration tooling.
