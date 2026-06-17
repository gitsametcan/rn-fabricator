# Tests

The test suite uses xUnit and is split by behavior:

- `ProductInfoTests`: stable product metadata.
- `CliCommandFactoryTests`: command registration and parse shape.
- `CliInvocationSmokeTests`: end-to-end command invocation smoke tests for current placeholder actions.
- `Doctor/*`: doctor command orchestration, output rendering, and exit code behavior.
- `Environment/*`: dependency check result models and dependency check services for the future `doctor` command, including core, Apple, and Android tool checks.
- `Projects/*`: create command validation and orchestration behavior.
- `Processes/*`: process execution abstractions and test doubles for future environment checks.
- `Templates/*`: local template package resolution and manifest behavior.

Console-output tests use the `ConsoleOutput` xUnit collection because `Console.Out` is process-global and must not be captured by multiple tests at the same time.

Run tests locally:

```bash
dotnet test rn-fabricator.sln --configuration Release
```
