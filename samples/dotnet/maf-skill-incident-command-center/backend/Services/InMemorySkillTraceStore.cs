using System.Collections.Concurrent;
using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public sealed class InMemorySkillTraceStore : ISkillTraceStore
{
    private readonly ConcurrentDictionary<string, List<SkillEvent>> _events = new(StringComparer.OrdinalIgnoreCase);

    public void AddEvent(SkillEvent skillEvent)
    {
        List<SkillEvent> runEvents = _events.GetOrAdd(skillEvent.RunId, _ => []);
        lock (runEvents)
        {
            runEvents.Add(skillEvent);
        }
    }

    public IReadOnlyList<SkillEvent> GetEvents(string runId)
    {
        if (!_events.TryGetValue(runId, out List<SkillEvent>? runEvents))
        {
            return [];
        }

        lock (runEvents)
        {
            return runEvents
                .OrderBy(e => e.Timestamp, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
