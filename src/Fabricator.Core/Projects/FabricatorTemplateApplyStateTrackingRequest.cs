using Fabricator.Core.Templates;

namespace Fabricator.Core.Projects;

public sealed record FabricatorTemplateApplyStateTrackingRequest(
    string ProjectDirectory,
    string TemplateSource,
    FabricatorTemplatePackage Package,
    FabricatorTemplateApplyResult ApplyResult);
