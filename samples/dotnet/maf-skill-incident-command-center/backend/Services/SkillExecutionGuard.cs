using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public static class SkillExecutionGuard
{
    public static bool HasLoadedSkill(IReadOnlyList<SkillEvent> events, string expectedSkillName)
    {
        if (string.IsNullOrWhiteSpace(expectedSkillName))
        {
            return false;
        }

        return events.Any(e =>
            string.Equals(e.Stage, "loaded", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.SkillName, expectedSkillName, StringComparison.OrdinalIgnoreCase));
    }
}
