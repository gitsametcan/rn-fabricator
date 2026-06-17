# Dogfooding Workflow

Dogfooding means testing rn-fabricator like a real user, outside this repository, before turning feedback into issues.

## Playground Location

Use a repo-external playground directory:

```bash
mkdir -p ~/Documents/rn-fabricator-playground
cd ~/Documents/rn-fabricator-playground
```

Do not generate React Native projects inside the rn-fabricator repository. Generated apps contain many files and can pollute the tool repository's git status.

## Install The Latest Release Package

Download the release package into the playground:

```bash
curl -L -o rn-fabricator.0.6.0-alpha.1.nupkg \
  https://github.com/gitsametcan/rn-fabricator/releases/download/v0.6.0-alpha.1/rn-fabricator.0.6.0-alpha.1.nupkg
```

Install it as a local tool:

```bash
dotnet tool install rn-fabricator \
  --tool-path ./.tools \
  --add-source . \
  --version 0.6.0-alpha.1
```

Verify the installed tool:

```bash
./.tools/rn-fabricator --version
./.tools/rn-fabricator --help
```

## Replace An Installed Playground Tool

Uninstall the current local tool:

```bash
dotnet tool uninstall rn-fabricator --tool-path ./.tools
```

Install a locally packed development version from the rn-fabricator repository:

```bash
dotnet tool install rn-fabricator \
  --tool-path ./.tools \
  --add-source /Users/sametcan/Documents/GitHub/fabricator/artifacts/packages \
  --version 0.6.0-alpha.1
```

Use a version override when packing an experimental build:

```bash
cd /Users/sametcan/Documents/GitHub/fabricator
dotnet pack src/Fabricator.Cli/Fabricator.Cli.csproj \
  --configuration Release \
  --output artifacts/packages \
  -p:VersionPrefix=0.6.0 \
  -p:VersionSuffix=dogfood.1
```

Then install that version from the playground:

```bash
cd ~/Documents/rn-fabricator-playground
dotnet tool install rn-fabricator \
  --tool-path ./.tools \
  --add-source /Users/sametcan/Documents/GitHub/fabricator/artifacts/packages \
  --version 0.6.0-dogfood.1
```

## Run Dogfooding Checks

Run the diagnostic command:

```bash
./.tools/rn-fabricator doctor
```

Preview guided setup actions without installing dependencies:

```bash
./.tools/rn-fabricator setup plan
```

Create a sample React Native project:

```bash
./.tools/rn-fabricator create FabricatorBabyStep --template basic-auth --output .
```

Inspect the generated project:

```bash
cd FabricatorBabyStep
ls
```

When the generated app is no longer needed, remove it from the playground:

```bash
cd ~/Documents/rn-fabricator-playground
rm -rf FabricatorBabyStep
```

## Feedback To Issue Workflow

Capture feedback as short, concrete observations:

```text
doctor: Missing Java hint should mention Temurin on macOS.
create: Failure output should show the exact command that failed.
template: Login screen needs loading state while sign-in is running.
docs: Playground install steps should mention tool uninstall first.
```

Turn each observation into a GitHub issue with:

- A user-facing title.
- The command that was run.
- The expected behavior.
- The actual behavior.
- The platform and tool version.
- Screenshots or terminal output when useful.

Assign issues to the active feedback milestone and label them by area, for example `area:cli`, `area:templates`, or `area:release`.

## Before Opening A Pull Request

For any dogfooding fix, run:

```bash
dotnet build rn-fabricator.sln --configuration Release
dotnet test rn-fabricator.sln --no-build --configuration Release
dotnet pack src/Fabricator.Cli/Fabricator.Cli.csproj --no-build --configuration Release --output artifacts/packages
```

If the fix changes user-facing behavior, reinstall the packed tool in the playground and repeat the relevant command.
