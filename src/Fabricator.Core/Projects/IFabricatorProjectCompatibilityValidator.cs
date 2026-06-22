namespace Fabricator.Core.Projects;

public interface IFabricatorProjectCompatibilityValidator
{
    FabricatorProjectCompatibilityResult Validate(string projectDirectory);
}
