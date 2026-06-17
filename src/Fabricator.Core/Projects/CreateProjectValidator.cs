using System.Text.RegularExpressions;

namespace Fabricator.Core.Projects;

public sealed class CreateProjectValidator
{
    private static readonly Regex ProjectNamePattern = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public CreateProjectValidationResult Validate(CreateProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        var projectName = request.ProjectName.Trim();
        var templateName = request.TemplateName.Trim();
        var outputDirectory = request.OutputDirectory.Trim();

        ValidateProjectName(projectName, errors);
        ValidateTemplateName(templateName, errors);

        var fullOutputDirectory = ResolveFullPath(outputDirectory, "output directory", errors);
        var fullProjectPath = string.Empty;

        if (!string.IsNullOrWhiteSpace(fullOutputDirectory))
        {
            ValidateOutputDirectory(fullOutputDirectory, errors);

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                fullProjectPath = Path.Combine(fullOutputDirectory, projectName);
                ValidateProjectPath(fullProjectPath, errors);
            }
        }

        return new CreateProjectValidationResult(
            request with
            {
                ProjectName = projectName,
                TemplateName = templateName,
                OutputDirectory = outputDirectory
            },
            fullOutputDirectory,
            fullProjectPath,
            errors);
    }

    private static void ValidateProjectName(string projectName, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            errors.Add("Project name is required.");
            return;
        }

        if (!ProjectNamePattern.IsMatch(projectName))
        {
            errors.Add("Project name must start with a letter and contain only letters, numbers, or underscores.");
        }

        if (projectName.Equals("React", StringComparison.OrdinalIgnoreCase) ||
            projectName.Equals("ReactNative", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Project name cannot be a reserved React Native identifier.");
        }
    }

    private static void ValidateTemplateName(string templateName, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            errors.Add("Template name is required.");
        }
    }

    private static string ResolveFullPath(string path, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"The {label} is required.");
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add($"The {label} path is invalid: {exception.Message}");
            return string.Empty;
        }
    }

    private static void ValidateOutputDirectory(string fullOutputDirectory, ICollection<string> errors)
    {
        if (File.Exists(fullOutputDirectory))
        {
            errors.Add("Output path points to a file. Provide an existing directory instead.");
            return;
        }

        if (!Directory.Exists(fullOutputDirectory))
        {
            errors.Add("Output directory does not exist. Create it first or choose an existing directory.");
        }
    }

    private static void ValidateProjectPath(string fullProjectPath, ICollection<string> errors)
    {
        if (Directory.Exists(fullProjectPath) || File.Exists(fullProjectPath))
        {
            errors.Add("Target project path already exists. Choose a different project name or output directory.");
        }
    }
}
