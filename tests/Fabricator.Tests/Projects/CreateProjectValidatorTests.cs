using Fabricator.Core.Projects;

namespace Fabricator.Tests.Projects;

public sealed class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator = new();

    [Fact]
    public void ValidateAcceptsValidProjectNameAndExistingOutputDirectory()
    {
        using var outputDirectory = new TemporaryDirectory();
        var request = new CreateProjectRequest("MyApp", "basic-auth", outputDirectory.Path);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(Path.Combine(outputDirectory.Path, "MyApp"), result.FullProjectPath);
    }

    [Theory]
    [InlineData("1App")]
    [InlineData("my-app")]
    [InlineData("My App")]
    [InlineData("My.App")]
    [InlineData("../MyApp")]
    public void ValidateRejectsInvalidProjectNames(string projectName)
    {
        using var outputDirectory = new TemporaryDirectory();
        var request = new CreateProjectRequest(projectName, "basic-auth", outputDirectory.Path);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            "Project name must start with a letter and contain only letters, numbers, or underscores.",
            result.Errors);
    }

    [Theory]
    [InlineData("React")]
    [InlineData("ReactNative")]
    public void ValidateRejectsReservedProjectNames(string projectName)
    {
        using var outputDirectory = new TemporaryDirectory();
        var request = new CreateProjectRequest(projectName, "basic-auth", outputDirectory.Path);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains("Project name cannot be a reserved React Native identifier.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsMissingOutputDirectory()
    {
        var missingOutputDirectory = Path.Combine(Path.GetTempPath(), $"rn-fabricator-{Guid.NewGuid():N}");
        var request = new CreateProjectRequest("MyApp", "basic-auth", missingOutputDirectory);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains("Output directory does not exist. Create it first or choose an existing directory.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsOutputPathThatPointsToFile()
    {
        using var outputDirectory = new TemporaryDirectory();
        var outputFile = Path.Combine(outputDirectory.Path, "output.txt");
        File.WriteAllText(outputFile, string.Empty);
        var request = new CreateProjectRequest("MyApp", "basic-auth", outputFile);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains("Output path points to a file. Provide an existing directory instead.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsExistingTargetProjectPath()
    {
        using var outputDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(outputDirectory.Path, "MyApp"));
        var request = new CreateProjectRequest("MyApp", "basic-auth", outputDirectory.Path);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            "Target project path already exists. Choose a different project name or output directory.",
            result.Errors);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rn-fabricator-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
