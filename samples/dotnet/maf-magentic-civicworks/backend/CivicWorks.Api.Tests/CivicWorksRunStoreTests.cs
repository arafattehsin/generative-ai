using CivicWorks.Api.Models;
using CivicWorks.Api.Services;
using Xunit;

namespace CivicWorks.Api.Tests;

public sealed class CivicWorksRunStoreTests
{
    [Fact]
    public void ActivityIsDeduplicatedBoundedAndIsolatedFromEarlierSnapshots()
    {
        var store = new CivicWorksRunStore();
        var original = store.Create();
        store.Update(original.Id, _ => { });
        Assert.True(store.TryGet(original.Id, out var unchanged));
        Assert.Single(unchanged!.Activity);

        for (int i = 0; i < 260; i++)
        {
            int round = i;
            store.Update(original.Id, state => state.StatusMessage = $"Update {round}");
        }

        Assert.True(store.TryGet(original.Id, out var current));
        Assert.Equal(240, current!.Activity.Count);
        Assert.Equal(261, current.Activity[^1].Sequence);
        Assert.Single(original.Activity);
        Assert.Equal("Investigation opened", original.Activity[0].Message);
        Assert.Equal(240, current.Activity.Select(item => item.Sequence).Distinct().Count());
    }

    [Fact]
    public void ToolOnlyEventDoesNotAttributeThePreviousEvidenceAgain()
    {
        var store = new CivicWorksRunStore();
        var original = store.Create();
        var evidence = new EvidenceItemSnapshot(
            "asset", 1, "AR-DN-44", "Asset read", "Register", "Alignment uncertain",
            EvidenceStatus.Checking, "Read", "Civil Assets Analyst", "Compare alignment");
        var recorded = store.Update(original.Id, state =>
        {
            state.ToolCallsUsed++;
            state.Evidence[evidence.Id] = evidence;
            state.FocusEvidenceId = evidence.Id;
            state.StatusMessage = evidence.Activity;
            state.ActiveActivity = evidence.Activity;
        });
        Assert.Equal("asset", recorded.Activity[^1].EvidenceId);
        var toolOnly = store.Update(original.Id, state => state.ToolCallsUsed++);
        Assert.Null(toolOnly.Activity[^1].EvidenceId);
        Assert.Equal("Read-only tool call completed", toolOnly.Activity[^1].Message);
    }

    [Fact]
    public void ApprovedPlanTextAndApprovalSurviveReplanning()
    {
        var store = new CivicWorksRunStore();
        var original = store.Create();
        store.Update(original.Id, state =>
        {
            state.PlanVersion = 1;
            state.PlanText = "Initial plan";
        });
        var approvedAt = DateTimeOffset.UtcNow;
        var approved = store.Update(original.Id, state =>
        {
            state.Plans[0] = state.Plans[0] with { ApprovedAt = approvedAt, Constraints = ["Read-only"] };
            state.PlanText = "Late task ledger";
        });
        Assert.Equal("Initial plan", approved.Plans[0].Text);
        var revised = store.Update(original.Id, state =>
        {
            state.PlanVersion = 2;
            state.PlanText = "Revised plan";
        });
        Assert.Equal(2, revised.Plans.Count);
        Assert.Equal(approvedAt, revised.Plans[0].ApprovedAt);
        Assert.Null(revised.Plans[1].ApprovedAt);
        Assert.Single(approved.Plans);
        Assert.Equal("Read-only", revised.Plans[0].Constraints[0]);
    }

    [Fact]
    public void CompletedRunUsesItsFinishTimeInsteadOfTheCurrentClock()
    {
        var store = new CivicWorksRunStore();
        var original = store.Create();
        Assert.True(store.TryGetState(original.Id, out var state));
        state!.Phase = RunPhase.Completed;
        state.UpdatedAt = state.StartedAt.AddSeconds(42);
        Assert.Equal(42, state.ToSnapshot().Budget.ElapsedSeconds);
    }
}