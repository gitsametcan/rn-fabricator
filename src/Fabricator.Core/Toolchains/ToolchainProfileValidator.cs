namespace Fabricator.Core.Toolchains;

public sealed class ToolchainProfileValidator
{
    public ToolchainProfileValidationResult Validate(ToolchainProfile? profile)
    {
        var errors = new List<string>();

        if (profile is null)
        {
            return ToolchainProfileValidationResult.Failed(["Toolchain profile is required."]);
        }

        AddRequiredTextError(errors, profile.Id, "Profile id is required.");
        AddRequiredTextError(errors, profile.DisplayName, "Profile display name is required.");
        AddRequiredTextError(errors, profile.ReactNativeVersion, "React Native version is required.");

        ValidateRequirement(errors, profile.NodeJs, "Node.js");
        ValidateRequirement(errors, profile.Java, "Java");
        ValidateRequirement(errors, profile.Xcode, "Xcode");
        ValidateRequirement(errors, profile.CocoaPods, "CocoaPods");
        ValidateRequirement(errors, profile.Watchman, "Watchman");
        ValidateAndroid(errors, profile.Android);

        return errors.Count == 0
            ? ToolchainProfileValidationResult.Success
            : ToolchainProfileValidationResult.Failed(errors);
    }

    private static void ValidateRequirement(
        List<string> errors,
        ToolchainRequirement? requirement,
        string fieldName)
    {
        if (requirement is null)
        {
            errors.Add($"{fieldName} requirement is required.");
            return;
        }

        AddRequiredTextError(errors, requirement.Name, $"{fieldName} requirement name is required.");

        switch (requirement.Strategy)
        {
            case ToolchainRecommendationStrategy.ExactVersion:
                AddRequiredTextError(errors, requirement.RecommendedVersion, $"{fieldName} exact version recommendation is required.");
                break;
            case ToolchainRecommendationStrategy.MinimumVersion:
                AddRequiredTextError(errors, requirement.MinimumVersion, $"{fieldName} minimum version is required.");
                break;
            case ToolchainRecommendationStrategy.SupportedRange:
                AddRequiredTextError(errors, requirement.SupportedVersionRange, $"{fieldName} supported version range is required.");
                break;
            case ToolchainRecommendationStrategy.LatestStable:
            case ToolchainRecommendationStrategy.Lts:
            case ToolchainRecommendationStrategy.Manual:
                break;
            default:
                errors.Add($"{fieldName} recommendation strategy is not supported.");
                break;
        }
    }

    private static void ValidateAndroid(List<string> errors, AndroidToolchainRequirement? android)
    {
        if (android is null)
        {
            errors.Add("Android requirement is required.");
            return;
        }

        if (android.CompileSdk <= 0)
        {
            errors.Add("Android compile SDK must be greater than zero.");
        }

        if (android.TargetSdk is <= 0)
        {
            errors.Add("Android target SDK must be greater than zero when provided.");
        }

        if (android.MinSdk is <= 0)
        {
            errors.Add("Android min SDK must be greater than zero when provided.");
        }

        if (android is { MinSdk: not null, TargetSdk: not null }
            && android.MinSdk > android.TargetSdk)
        {
            errors.Add("Android min SDK must not be greater than target SDK.");
        }

        ValidateRequirement(errors, android.PlatformTools, "Android platform tools");
    }

    private static void AddRequiredTextError(List<string> errors, string? value, string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(error);
        }
    }
}
