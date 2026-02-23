using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public interface ISkillTraceStore
{
    void AddEvent(SkillEvent skillEvent);
    IReadOnlyList<SkillEvent> GetEvents(string runId);
}
