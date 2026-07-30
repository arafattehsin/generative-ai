using TravelConcierge.Api.Models;
using TravelConcierge.Api.Services;

namespace TravelConcierge.Api.Tests;

public sealed class RunStoreTests
{
    [Fact]
    public void HumanRequestCarriesContextAndMovesOwnershipToRecoveryOperations()
    {
        RunStore store = new();
        TravelRun run = CreateRun(store);
        HumanSupportRequest request = CreateHumanRequest(run.Id);

        store.RequestHumanSupport(run.Id, "request-17", request);

        Assert.Equal(RunStatus.WaitingForHuman, run.Status);
        Assert.Equal(TravelAgents.Human.Id, run.CurrentOwnerId);
        Assert.Equal(TravelAgents.Human.Name, run.CurrentOwnerName);
        Assert.NotNull(run.HumanSupport);
        Assert.Equal("Flight Recovery", run.HumanSupport.RequestedByAgentName);
        Assert.Equal("Confirm the fastest authorised option.", run.HumanSupport.DecisionNeeded);
        Assert.Equal(HumanSupportStatus.Requested, run.HumanSupport.Status);
    }

    [Fact]
    public void ClaimNamesTheOperatorAndPreventsAnotherOperatorTakingTheCase()
    {
        RunStore store = new();
        TravelRun run = CreateRun(store);
        store.RequestHumanSupport(run.Id, "request-17", CreateHumanRequest(run.Id));

        bool claimedByAlex = store.ClaimHumanSupport(run.Id, "Alex Morgan");
        bool claimedBySomeoneElse = store.ClaimHumanSupport(run.Id, "Jordan Lee");

        Assert.True(claimedByAlex);
        Assert.False(claimedBySomeoneElse);
        Assert.Equal(RunStatus.HumanResponding, run.Status);
        Assert.Equal("Alex Morgan", run.CurrentOwnerName);
        Assert.Equal("Alex Morgan", run.HumanSupport?.AssignedTo);
        Assert.NotNull(run.HumanSupport?.ClaimedAt);
    }

    [Fact]
    public void HumanDecisionAppearsBeforeOwnershipReturnsToTheSelectedAgent()
    {
        RunStore store = new();
        TravelRun run = CreateRun(store);
        store.RequestHumanSupport(run.Id, "request-17", CreateHumanRequest(run.Id));
        store.ClaimHumanSupport(run.Id, "Alex Morgan");
        HumanSupportResolution resolution = new(
            "Alex Morgan",
            "Use the first confirmed partner itinerary that arrives before 2 PM.",
            TravelAgents.Insurance.Id);

        store.ResolveHumanSupport(run.Id, resolution);
        store.AddHumanMessage(run.Id, resolution.OperatorName, resolution.Message);
        store.AddHandoff(run.Id, TravelAgents.Human, TravelAgents.Insurance, "Human decision complete.");
        store.SetStatus(run.Id, RunStatus.WaitingForUser);

        ConversationMessage humanMessage = Assert.Single(run.Messages, message => message.AuthorType == ConversationAuthor.Human);
        Assert.Equal("Alex Morgan", humanMessage.AuthorName);
        Assert.Equal(HumanSupportStatus.Resolved, run.HumanSupport?.Status);
        Assert.NotNull(run.HumanSupport?.ResolvedAt);
        Assert.Equal(TravelAgents.Insurance.Id, run.CurrentOwnerId);
        Assert.Equal(TravelAgents.Insurance.Name, run.CurrentOwnerName);
        Assert.Equal(RunStatus.WaitingForUser, run.Status);
        Assert.Equal(TravelAgents.Human.Id, run.Timeline[^1].FromAgentId);
        Assert.Equal(TravelAgents.Insurance.Id, run.Timeline[^1].ToAgentId);
    }

    private static TravelRun CreateRun(RunStore store) => store.Create(new CreateRunRequest(
        "I need the earliest confirmed way home before my medical appointment.",
        "Maya Chen",
        "SYD-LAX-4817",
        "Critical"));

    private static HumanSupportRequest CreateHumanRequest(Guid runId) => new(
        runId,
        "Maya Chen",
        "SYD-LAX-4817",
        "Critical",
        TravelAgents.Flight.Id,
        TravelAgents.Flight.Name,
        "No confirmed rebooking and a medical deadline.",
        "Confirm the fastest authorised option.",
        "Prioritise arrival before 2 PM, including partner inventory.",
        "QF11 was cancelled; Maya is at SYD with one checked bag.");
}
