using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public interface ISkillCatalogService
{
    IReadOnlyList<SkillSummary> GetSkills();
    string ResolveSkillName(string skillName);
    string ReadSkillDefinition(string skillName);
    IReadOnlyList<string> GetResources(string skillName);
    string ReadResource(string skillName, string resourcePath);
}
