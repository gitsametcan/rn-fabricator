using Fabricator.Cli.Setup;
using Fabricator.Core;
using Fabricator.Core.Processes;
using Fabricator.Core.Setup;

namespace Fabricator.Tests.Setup;

public sealed class SetupApplyCommandHandlerTests
{
    [Fact]
    public async Task RunAsyncExecutesAcceptedSafeCommand()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = CreatePlan([
                new SetupPlanItem(
                    "Watchman",
                    SetupPlanItemKind.Command,
                    "Install Watchman",
                    ["brew install watchman"])
            ])
        };
        var processRunner = new FakeProcessRunner();
        processRunner.Enqueue(new ProcessRunResult(ExitCodes.Success, "installed", string.Empty));
        using var writer = new StringWriter();
        using var errorWriter = new StringWriter();
        using var reader = new StringReader("y\n");
        var handler = CreateHandler(setupPlanService, processRunner, reader, writer, errorWriter);

        var exitCode = await handler.RunAsync();

        Assert.Equal(ExitCodes.Success, exitCode);
        var request = Assert.Single(processRunner.Requests);
        Assert.Equal("brew", request.FileName);
        Assert.Equal(["install", "watchman"], request.Arguments);
        Assert.Contains("[succeeded] Watchman", writer.ToString());
        Assert.DoesNotContain("No install commands were executed.", writer.ToString());
        Assert.Contains("Apply summary: 1 succeeded, 0 failed, 0 skipped by user, 0 skipped by policy, 0 manual.", writer.ToString());
        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public async Task RunAsyncSkipsDeclinedCommand()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = CreatePlan([
                new SetupPlanItem(
                    "Watchman",
                    SetupPlanItemKind.Command,
                    "Install Watchman",
                    ["brew install watchman"])
            ])
        };
        var processRunner = new FakeProcessRunner();
        using var writer = new StringWriter();
        using var reader = new StringReader("\n");
        var handler = CreateHandler(setupPlanService, processRunner, reader, writer);

        var exitCode = await handler.RunAsync();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Empty(processRunner.Requests);
        Assert.Contains("[skipped by user] Watchman", writer.ToString());
        Assert.Contains("Apply summary: 0 succeeded, 0 failed, 1 skipped by user, 0 skipped by policy, 0 manual.", writer.ToString());
    }

    [Fact]
    public async Task RunAsyncDryRunDoesNotPromptOrExecuteSafeCommand()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = CreatePlan([
                new SetupPlanItem(
                    "Watchman",
                    SetupPlanItemKind.Command,
                    "Install Watchman",
                    ["brew install watchman"])
            ])
        };
        var processRunner = new FakeProcessRunner();
        using var writer = new StringWriter();
        using var reader = new StringReader(string.Empty);
        var handler = CreateHandler(setupPlanService, processRunner, reader, writer);

        var exitCode = await handler.RunAsync(dryRun: true);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Empty(processRunner.Requests);
        var output = writer.ToString();
        Assert.Contains("Mode: dry-run (no commands will be executed).", output);
        Assert.Contains("[dry-run] Watchman: would run brew install watchman", output);
        Assert.DoesNotContain("Run this command?", output);
        Assert.Contains("Apply summary: 0 succeeded, 0 failed, 0 skipped by user, 0 skipped by policy, 0 manual, 1 would run.", output);
    }

    [Fact]
    public async Task RunAsyncYesExecutesSafeCommandWithoutPrompt()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = CreatePlan([
                new SetupPlanItem(
                    "Watchman",
                    SetupPlanItemKind.Command,
                    "Install Watchman",
                    ["brew install watchman"])
            ])
        };
        var processRunner = new FakeProcessRunner();
        processRunner.Enqueue(new ProcessRunResult(ExitCodes.Success, "installed", string.Empty));
        using var writer = new StringWriter();
        using var reader = new StringReader(string.Empty);
        var handler = CreateHandler(setupPlanService, processRunner, reader, writer);

        var exitCode = await handler.RunAsync(yes: true);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Single(processRunner.Requests);
        var output = writer.ToString();
        Assert.Contains("Mode: yes (safe allowlisted commands will run without prompts).", output);
        Assert.Contains("[succeeded] Watchman", output);
        Assert.DoesNotContain("Run this command?", output);
        Assert.Contains("Apply summary: 1 succeeded, 0 failed, 0 skipped by user, 0 skipped by policy, 0 manual.", output);
    }

    [Fact]
    public async Task RunAsyncReturnsFailureWhenAcceptedCommandFails()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = CreatePlan([
                new SetupPlanItem(
                    "Watchman",
                    SetupPlanItemKind.Command,
                    "Install Watchman",
                    ["brew install watchman"])
            ])
        };
        var processRunner = new FakeProcessRunner();
        processRunner.Enqueue(new ProcessRunResult(42, string.Empty, "install failed"));
        using var writer = new StringWriter();
        using var reader = new StringReader("yes\n");
        var handler = CreateHandler(setupPlanService, processRunner, reader, writer);

        var exitCode = await handler.RunAsync();

        Assert.Equal(ExitCodes.GeneralFailure, exitCode);
        Assert.Single(processRunner.Requests);
        Assert.Contains("[failed] Watchman: exit code 42", writer.ToString());
        Assert.Contains("install failed", writer.ToString());
        Assert.Contains("Apply summary: 0 succeeded, 1 failed, 0 skipped by user, 0 skipped by policy, 0 manual.", writer.ToString());
    }

    [Fact]
    public async Task RunAsyncSkipsElevatedAndManualSteps()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = CreatePlan([
                new SetupPlanItem(
                    "CocoaPods",
                    SetupPlanItemKind.Command,
                    "Install CocoaPods",
                    ["sudo gem install cocoapods"],
                    RequiresAdmin: true),
                new SetupPlanItem(
                    "Xcode",
                    SetupPlanItemKind.Manual,
                    "Install Xcode",
                    ["Install Xcode from the App Store."]),
                new SetupPlanItem(
                    "Android SDK",
                    SetupPlanItemKind.Environment,
                    "Configure Android SDK",
                    ["Set ANDROID_HOME."])
            ])
        };
        var processRunner = new FakeProcessRunner();
        using var writer = new StringWriter();
        using var reader = new StringReader("y\n");
        var handler = CreateHandler(setupPlanService, processRunner, reader, writer);

        var exitCode = await handler.RunAsync();

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Empty(processRunner.Requests);
        Assert.Contains("[skipped by policy] CocoaPods: command requires admin privileges.", writer.ToString());
        Assert.Contains("[manual] Xcode: review plan output.", writer.ToString());
        Assert.Contains("[manual] Android SDK: review plan output.", writer.ToString());
        Assert.Contains("Apply summary: 0 succeeded, 0 failed, 0 skipped by user, 1 skipped by policy, 2 manual.", writer.ToString());
        Assert.Contains("Manual or skipped steps may still be required", writer.ToString());
    }

    [Fact]
    public async Task RunAsyncYesSkipsElevatedAndManualSteps()
    {
        var setupPlanService = new FakeSetupPlanService
        {
            Plan = CreatePlan([
                new SetupPlanItem(
                    "CocoaPods",
                    SetupPlanItemKind.Command,
                    "Install CocoaPods",
                    ["sudo gem install cocoapods"],
                    RequiresAdmin: true),
                new SetupPlanItem(
                    "Android SDK",
                    SetupPlanItemKind.Environment,
                    "Configure Android SDK",
                    ["Set ANDROID_HOME."])
            ])
        };
        var processRunner = new FakeProcessRunner();
        using var writer = new StringWriter();
        using var reader = new StringReader(string.Empty);
        var handler = CreateHandler(setupPlanService, processRunner, reader, writer);

        var exitCode = await handler.RunAsync(yes: true);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Empty(processRunner.Requests);
        var output = writer.ToString();
        Assert.Contains("[skipped by policy] CocoaPods: command requires admin privileges.", output);
        Assert.Contains("[manual] Android SDK: review plan output.", output);
        Assert.Contains("Apply summary: 0 succeeded, 0 failed, 0 skipped by user, 1 skipped by policy, 1 manual.", output);
    }

    [Fact]
    public async Task RunAsyncReturnsInvalidInputForMutuallyExclusiveProfileOptions()
    {
        var setupPlanService = new FakeSetupPlanService();
        using var writer = new StringWriter();
        using var errorWriter = new StringWriter();
        using var reader = new StringReader(string.Empty);
        var handler = CreateHandler(setupPlanService, new FakeProcessRunner(), reader, writer, errorWriter);

        var exitCode = await handler.RunAsync("react-native-stable", "0.76.x");

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Equal(0, setupPlanService.CallCount);
        Assert.Contains("Choose either --profile or --react-native, not both.", errorWriter.ToString());
    }

    [Fact]
    public async Task RunAsyncReturnsInvalidInputForMutuallyExclusiveApplyModes()
    {
        var setupPlanService = new FakeSetupPlanService();
        using var writer = new StringWriter();
        using var errorWriter = new StringWriter();
        using var reader = new StringReader(string.Empty);
        var handler = CreateHandler(setupPlanService, new FakeProcessRunner(), reader, writer, errorWriter);

        var exitCode = await handler.RunAsync(dryRun: true, yes: true);

        Assert.Equal(ExitCodes.InvalidInput, exitCode);
        Assert.Equal(0, setupPlanService.CallCount);
        Assert.Contains("Choose either --dry-run or --yes, not both.", errorWriter.ToString());
    }

    private static SetupApplyCommandHandler CreateHandler(
        FakeSetupPlanService setupPlanService,
        FakeProcessRunner processRunner,
        TextReader reader,
        TextWriter writer,
        TextWriter? errorWriter = null)
    {
        return new SetupApplyCommandHandler(
            setupPlanService,
            new SetupPlanRenderer(writer),
            processRunner,
            reader,
            writer,
            errorWriter ?? TextWriter.Null);
    }

    private static SetupPlan CreatePlan(IReadOnlyList<SetupPlanItem> items)
    {
        return new SetupPlan(
            "macOS",
            new PackageManagerInfo("Homebrew", true, "brew"),
            items,
            new SetupPlanToolchainProfile(
                "react-native-stable",
                "React Native Stable",
                "0.76.x",
                IsDefault: true));
    }
}
