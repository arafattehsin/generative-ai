using IncidentCommandCenter.Api.Models;
using IncidentCommandCenter.Api.Services;
using Xunit;

namespace IncidentCommandCenter.Api.Tests;

public sealed class SkillExecutionGuardTests
{
    [Fact]
    public void HasLoadedSkill_ReturnsTrue_WhenExpectedSkillWasLoaded()
    {
        IReadOnlyList<SkillEvent> events =
        [
            new("run-1", "2026-02-21T10:00:00.0000000Z", "advertised", "incident-triage", null, null),
            new("run-1", "2026-02-21T10:00:01.0000000Z", "loaded", "incident-triage", null, null),
            new("run-1", "2026-02-21T10:00:02.0000000Z", "completed", "incident-triage", null, null),
        ];

        bool result = SkillExecutionGuard.HasLoadedSkill(events, "incident-triage");

        Assert.True(result);
    }

    [Fact]
    public void HasLoadedSkill_ReturnsFalse_WhenSkillWasNotLoaded()
    {
        IReadOnlyList<SkillEvent> events =
        [
            new("run-1", "2026-02-21T10:00:00.0000000Z", "advertised", "incident-triage", null, null),
            new("run-1", "2026-02-21T10:00:01.0000000Z", "resource_read", "incident-triage", "references/SLA_POLICY.md", null),
            new("run-1", "2026-02-21T10:00:02.0000000Z", "completed", "incident-triage", null, null),
        ];

        bool result = SkillExecutionGuard.HasLoadedSkill(events, "incident-triage");

        Assert.False(result);
    }
}
