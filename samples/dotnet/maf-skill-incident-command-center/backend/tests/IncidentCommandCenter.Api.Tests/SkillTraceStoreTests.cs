using IncidentCommandCenter.Api.Models;
using IncidentCommandCenter.Api.Services;
using Xunit;

namespace IncidentCommandCenter.Api.Tests;

public sealed class SkillTraceStoreTests
{
    [Fact]
    public void Events_AreReturnedInTimestampOrder()
    {
        ISkillTraceStore store = new InMemorySkillTraceStore();
        string runId = "run-123";

        store.AddEvent(new SkillEvent(runId, "2026-02-21T10:00:02.0000000Z", "completed", "incident-triage", null, null));
        store.AddEvent(new SkillEvent(runId, "2026-02-21T10:00:00.0000000Z", "advertised", "incident-triage", null, null));
        store.AddEvent(new SkillEvent(runId, "2026-02-21T10:00:01.0000000Z", "loaded", "incident-triage", null, null));

        var events = store.GetEvents(runId);

        Assert.Equal("advertised", events[0].Stage);
        Assert.Equal("loaded", events[1].Stage);
        Assert.Equal("completed", events[2].Stage);
    }
}
