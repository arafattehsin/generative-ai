using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public static class SkillEventFactory
{
    public static SkillEvent Create(
        string runId,
        string stage,
        string skillName,
        string? resource,
        string? note)
    {
        return new SkillEvent(
            RunId: runId,
            Timestamp: DateTimeOffset.UtcNow.ToString("O"),
            Stage: stage,
            SkillName: skillName,
            Resource: resource,
            Note: note
        );
    }
}
