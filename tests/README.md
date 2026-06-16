# Tests

The test suite uses xUnit and is split by behavior:

- `ProductInfoTests`: stable product metadata.
- `CliCommandFactoryTests`: command registration and parse shape.
- `CliInvocationSmokeTests`: end-to-end command invocation smoke tests for current placeholder actions.
- `Processes/*`: process execution abstractions and test doubles for future environment checks.

Console-output tests use the `ConsoleOutput` xUnit collection because `Console.Out` is process-global and must not be captured by multiple tests at the same time.

Run tests locally:

```bash
dotnet test rn-fabricator.sln --configuration Release
```
