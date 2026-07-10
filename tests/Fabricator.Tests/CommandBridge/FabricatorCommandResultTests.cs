using Fabricator.Core;
using Fabricator.Core.CommandBridge;
using Fabricator.Core.Processes;

namespace Fabricator.Tests.CommandBridge;

public sealed class FabricatorCommandResultTests
{
    [Fact]
    public void FromProcessResultPreservesSuccessfulExitCodeAndOutput()
    {
        var request = FabricatorCommandRequest.Create("doctor");
        var processResult = new ProcessRunResult(
            ExitCodes.Success,
            "doctor ok",
            string.Empty);

        var result = FabricatorCommandResult.FromProcessResult(request, processResult);

        Assert.True(result.Succeeded);
        Assert.Equal(FabricatorCommandState.Completed, result.State);
        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Equal("doctor ok", result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FromProcessResultPreservesFailureExitCodeAndActionableError()
    {
        var request = FabricatorCommandRequest.Create("setup", "apply");
        var processResult = new ProcessRunResult(
            42,
            "installing",
            "install failed");

        var result = FabricatorCommandResult.FromProcessResult(request, processResult);

        Assert.False(result.Succeeded);
        Assert.Equal(FabricatorCommandState.Failed, result.State);
        Assert.Equal(42, result.ExitCode);
        Assert.Equal("installing", result.StandardOutput);
        Assert.Equal("install failed", result.StandardError);
        Assert.NotNull(result.Error);
        Assert.Equal(FabricatorCommandErrorKind.ProcessFailure, result.Error.Kind);
        Assert.Equal(42, result.Error.ExitCode);
        Assert.Contains("retry", result.Error.RecoveryHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidInputUsesInvalidInputExitCodeAndErrorKind()
    {
        var request = FabricatorCommandRequest.Create("create", "my-app");

        var result = FabricatorCommandResult.InvalidInput(
            request,
            "Project name is invalid.",
            "Use PascalCase.",
            "Rename the project and retry.");

        Assert.False(result.Succeeded);
        Assert.Equal(FabricatorCommandState.Failed, result.State);
        Assert.Equal(ExitCodes.InvalidInput, result.ExitCode);
        Assert.Equal("Use PascalCase.", result.StandardError);
        Assert.NotNull(result.Error);
        Assert.Equal(FabricatorCommandErrorKind.InvalidInput, result.Error.Kind);
        Assert.Equal("Rename the project and retry.", result.Error.RecoveryHint);
    }

    [Fact]
    public void CanceledDoesNotInventExitCode()
    {
        var request = FabricatorCommandRequest.Create("create", "MyApp");

        var result = FabricatorCommandResult.Canceled(request);

        Assert.False(result.Succeeded);
        Assert.Equal(FabricatorCommandState.Canceled, result.State);
        Assert.Null(result.ExitCode);
        Assert.NotNull(result.Error);
        Assert.Equal(FabricatorCommandErrorKind.Canceled, result.Error.Kind);
    }
}
