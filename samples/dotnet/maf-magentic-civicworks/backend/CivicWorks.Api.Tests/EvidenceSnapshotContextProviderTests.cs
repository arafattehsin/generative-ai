using CivicWorks.Api.Models;
using CivicWorks.Api.Services;
using Xunit;

namespace CivicWorks.Api.Tests;

public sealed class EvidenceSnapshotContextProviderTests
{
    [Fact]
    public void EmptyInvestigationPreservesCommunityCollectionBeforeConflictChecks()
    {
        string snapshot = EvidenceSnapshotContextProvider.BuildSnapshot(new CivicWorksRunState());
        Assert.Contains("Collected evidence: none.", snapshot);
        Assert.Contains("Missing baseline evidence: complaints, access", snapshot);
        Assert.Contains("Select CommunityAccessAnalyst before collecting civil assets", snapshot);
        Assert.DoesNotContain("Baseline service-request and access evidence is present", snapshot);
    }

    [Fact]
    public void ReplanCanRecoverCommunityEvidenceThatWasNeverCollected()
    {
        var state = StateWithEvidence("asset", "sandstone", "constraints", "heritage", "survey", "cost");
        state.PlanVersion = 2;
        string snapshot = EvidenceSnapshotContextProvider.BuildSnapshot(state);
        Assert.Contains("Missing baseline evidence: complaints, access", snapshot);
        Assert.Contains("Select CommunityAccessAnalyst", snapshot);
        Assert.Contains("EvidenceVerifier completion prerequisites still missing: complaints, access", snapshot);
        Assert.DoesNotContain("Do not repeat Community & Access", snapshot);
        Assert.DoesNotContain("All verification prerequisites are present", snapshot);
    }

    [Fact]
    public void PartialCommunityEvidenceOnlyRequestsTheMissingRecord()
    {
        var state = StateWithEvidence("complaints", "asset", "sandstone");
        state.PlanVersion = 2;
        string snapshot = EvidenceSnapshotContextProvider.BuildSnapshot(state);
        Assert.Contains("Missing baseline evidence: access.", snapshot);
        Assert.DoesNotContain("Missing baseline evidence: complaints", snapshot);
    }

    [Fact]
    public void CompletePacketRetainsEvidenceAndRequestsTheActualVerifierTool()
    {
        var state = StateWithEvidence("complaints", "access", "asset", "sandstone", "constraints", "heritage", "survey", "cost");
        state.PlanVersion = 2;
        string snapshot = EvidenceSnapshotContextProvider.BuildSnapshot(state);
        Assert.DoesNotContain("Missing baseline evidence", snapshot);
        Assert.Contains("All verification prerequisites are present", snapshot);
        Assert.Contains("Require EvidenceVerifier to run its tool", snapshot);
        Assert.Contains("Do not trigger another reset from historical", snapshot);
    }

    [Fact]
    public void VerifiedPacketMovesToSynthesisInsteadOfAnotherReadinessHandoff()
    {
        var state = StateWithEvidence("complaints", "access", "asset", "sandstone", "constraints", "heritage", "survey", "cost", "verification");
        state.PlanVersion = 2;
        string snapshot = EvidenceSnapshotContextProvider.BuildSnapshot(state);
        Assert.Contains("is_request_satisfied=true", snapshot);
        Assert.Contains("final-answer synthesis", snapshot);
        Assert.Contains("No further verifier readiness confirmation", snapshot);
        Assert.DoesNotContain("Require EvidenceVerifier to run its tool", snapshot);
    }

    [Fact]
    public void VerificationRecordCannotHideMissingPrerequisites()
    {
        var state = StateWithEvidence("asset", "sandstone", "verification");
        state.PlanVersion = 2;
        string snapshot = EvidenceSnapshotContextProvider.BuildSnapshot(state);
        Assert.DoesNotContain("is_request_satisfied=true", snapshot);
        Assert.Contains("EvidenceVerifier completion prerequisites still missing: complaints, access", snapshot);
    }

    private static CivicWorksRunState StateWithEvidence(params string[] ids)
    {
        var state = new CivicWorksRunState();
        foreach (string id in ids)
        {
            state.Evidence[id] = new EvidenceItemSnapshot(id, state.Evidence.Count + 1,
                "REF-" + id, id, "Synthetic source", "Recorded finding", EvidenceStatus.Verified,
                "Collected", "Specialist", "Read-only tool returned");
        }
        return state;
    }
}
