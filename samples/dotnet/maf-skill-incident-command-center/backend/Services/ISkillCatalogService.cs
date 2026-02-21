using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public interface ISkillCatalogService
{
    IReadOnlyList<SkillSummary> GetSkills();
    IReadOnlyList<string> GetResources(string skillName);
    string ReadResource(string skillName, string resourcePath);
}
