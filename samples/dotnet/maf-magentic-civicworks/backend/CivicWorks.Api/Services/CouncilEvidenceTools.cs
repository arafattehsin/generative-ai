using System.Text.Json;
using CivicWorks.Api.Models;

namespace CivicWorks.Api.Services;

public sealed class CouncilEvidenceTools(
    Guid runId,
    CivicWorksRunStore store,
    RunNotifier notifier)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public Task<string> GetServiceRequestsAsync() => RecordEvidenceAsync(
        new EvidenceItemSnapshot(
            "complaints",
            1,
            "SR-1182 · SR-1214 · SR-1260 · SR-1271",
            "Recurring ponding pattern",
            "Synthetic service request register",
            "Four fictional requests locate ponding at the library crossing after moderate rainfall. Resident details are excluded.",
            EvidenceStatus.Verified,
            "Verified by read-only tool",
            "Community & Access Analyst",
            "4 records linked"),
        new
        {
            caseId = "CW-2047",
            evidenceReference = "SR-1182, SR-1214, SR-1260, SR-1271",
            finding = "Four service requests independently locate recurring ponding at the library crossing after moderate rainfall.",
            privacy = "Synthetic records; no resident names, addresses, phone numbers, or free-text identifiers.",
            status = "verified",
        });

    public Task<string> GetAccessAuditAsync() => RecordEvidenceAsync(
        new EvidenceItemSnapshot(
            "access",
            2,
            "AR-ACCESS-12",
            "Accessible route is incomplete",
            "Synthetic access audit",
            "The continuous path between the library, community centre, and bus stop is interrupted by uneven crossfall and a missing kerb transition.",
            EvidenceStatus.Verified,
            "Verified by read-only tool",
            "Community & Access Analyst",
            "Access impact confirmed"),
        new
        {
            caseId = "CW-2047",
            evidenceReference = "AR-ACCESS-12",
            finding = "The continuous accessible path is interrupted by uneven crossfall and a missing kerb transition.",
            affectedDestinations = new[] { "Marrin Library", "Community Centre", "Bus stop 214" },
            status = "verified",
        });

    public Task<string> GetCivilAssetRecordAsync() => RecordEvidenceAsync(
        new EvidenceItemSnapshot(
            "asset",
            3,
            "AR-DN-44",
            "Asset register records a piped drain",
            "Synthetic civil assets register",
            "The register describes a concrete piped drain through the eastern verge. Its mapped alignment must be compared with the current site observation.",
            EvidenceStatus.Checking,
            "Read from asset register",
            "Civil Assets Analyst",
            "Alignment comparison required"),
        new
        {
            caseId = "CW-2047",
            evidenceReference = "AR-DN-44",
            assetType = "Concrete piped drain",
            mappedAlignment = "Eastern verge beside the library crossing",
            reliability = "Legacy record; alignment has not been field-verified in this investigation.",
            requiredNextCheck = "Compare with site observation OBS-07 before assuming excavation conditions.",
            status = "checking",
        });

    public Task<string> GetSiteObservationAsync()
    {
        bool revisedPlanAlreadyExists = HasRevisedPlan();
        return RecordEvidenceAsync(
            new EvidenceItemSnapshot(
                "sandstone",
                4,
                "OBS-07",
                "Possible sandstone drain observed",
                "Synthetic site observation",
                "A linear sandstone feature is visible beside the ponding location. It conflicts with the mapped piped-drain alignment but is not itself a heritage finding.",
                EvidenceStatus.Conflict,
                revisedPlanAlreadyExists ? "Conflict retained in revised plan" : "Material conflict published",
                "Place & Constraints Advisor",
                revisedPlanAlreadyExists ? "Revised checks remain required" : "Current direct-works route invalidated"),
            new
            {
                caseId = "CW-2047",
                evidenceReference = "OBS-07",
                finding = "A linear sandstone feature is visible beside the ponding location.",
                conflictsWith = "AR-DN-44 describes a concrete piped drain on a different alignment.",
                limitation = "This observation is not a heritage determination.",
                orchestrationSignal = revisedPlanAlreadyExists ? "CONFLICT_TRACKED_IN_REVISED_PLAN" : "PLAN_INVALIDATED",
                requiredReplan = new[]
                {
                    "Verify the synthetic heritage register",
                    "Obtain a non-invasive site survey brief",
                    "Develop an excavation-avoiding option",
                    "Recalculate indicative cost and disruption",
                    "Challenge the revised brief against its evidence",
                },
                status = "conflict",
            },
            focusEvidenceId: "sandstone");
    }

    public Task<string> GetTreeAndAccessConstraintsAsync() => RecordEvidenceAsync(
        new EvidenceItemSnapshot(
            "constraints",
            5,
            "PC-22",
            "Place constraints apply",
            "Synthetic place constraints schedule",
            "Works must retain business access, protect two established street trees, and maintain a continuous accessible temporary route.",
            EvidenceStatus.Verified,
            "Verified by read-only tool",
            "Place & Constraints Advisor",
            "3 delivery constraints linked"),
        new
        {
            caseId = "CW-2047",
            evidenceReference = "PC-22",
            constraints = new[]
            {
                "Protect two established street trees and their root zones.",
                "Maintain business access during investigation and works.",
                "Maintain a continuous accessible temporary route between all three civic destinations.",
            },
            status = "verified",
        });

    public Task<string> GetHeritageRegisterCheckAsync() => RecordEvidenceAsync(
        new EvidenceItemSnapshot(
            "heritage",
            6,
            "HR-CHECK-09",
            "Heritage register check completed",
            "Read-only synthetic heritage register",
            "No listed item is returned for the observed feature, but the synthetic register warns that an unlisted sandstone drainage feature still requires qualified assessment before disturbance.",
            EvidenceStatus.Verified,
            "Register checked; matter remains open",
            "Place & Constraints Advisor",
            "No listing found; assessment still required"),
        new
        {
            caseId = "CW-2047",
            evidenceReference = "HR-CHECK-09",
            listedItemFound = false,
            finding = "The fictional register contains no listing for the observed feature.",
            limitation = "Absence from the register is not permission to disturb the feature and is not a heritage assessment.",
            requiredCondition = "Qualified assessment before any option assumes excavation beside OBS-07.",
            status = "verified_with_open_matter",
        });

    public Task<string> GetNonInvasiveSurveyBriefAsync() => RecordEvidenceAsync(
        new EvidenceItemSnapshot(
            "survey",
            7,
            "SURVEY-04",
            "Non-invasive survey route defined",
            "Synthetic survey planning record",
            "A ground survey and service-location check can test the alignment without excavation while retaining pedestrian and business access.",
            EvidenceStatus.Verified,
            "Survey method checked",
            "Place & Constraints Advisor",
            "Non-invasive method identified"),
        new
        {
            caseId = "CW-2047",
            evidenceReference = "SURVEY-04",
            method = "Ground survey plus non-destructive service-location check",
            purpose = "Resolve the asset alignment and sandstone-feature relationship without excavation.",
            accessControls = new[]
            {
                "Keep one continuous accessible path open.",
                "Schedule short observation windows outside library arrival peaks.",
                "Do not enter mapped tree protection zones.",
            },
            status = "verified",
        });

    public async Task<string> EstimateWorksOptionsAsync()
    {
        bool prerequisitesPresent = HasEvidence("heritage") && HasEvidence("survey") && HasEvidence("constraints");

        if (!prerequisitesPresent)
        {
            return await RecordToolOnlyAsync(new
            {
                caseId = "CW-2047",
                status = "blocked",
                orchestrationSignal = "PLAN_INVALIDATED",
                reason = "Indicative options cannot be estimated until HR-CHECK-09, SURVEY-04, and PC-22 have been obtained.",
            }).ConfigureAwait(false);
        }

        return await RecordEvidenceAsync(
            new EvidenceItemSnapshot(
                "cost",
                8,
                "COST-IND-03",
                "Indicative options compared",
                "Synthetic cost and disruption rules",
                "The evidence supports investigation before works. Surface repair leaves the drainage conflict unresolved; integrated renewal assumes excavation too early.",
                EvidenceStatus.Verified,
                "Indicative comparison complete",
                "Cost & Delivery Analyst",
                "3 fictional options compared"),
            new
            {
                caseId = "CW-2047",
                evidenceReference = "COST-IND-03",
                currency = "AUD",
                disclaimer = "Fictional indicative ranges for demonstration only; not an estimate, quotation, or expenditure approval.",
                options = new object[]
                {
                    new { code = "A", title = "Repair known surface defects", range = "$45k-$70k", assessment = "Lower immediate disruption but leaves the drainage alignment conflict unresolved." },
                    new { code = "B", title = "Renew route and drainage together", range = "$310k-$480k", assessment = "Potentially complete but currently relies on an unverified excavation assumption." },
                    new { code = "C", title = "Investigate first", range = "$18k-$32k", assessment = "Best-supported next step; resolves the material unknown before costed works design." },
                },
                supportedRecommendation = "C",
                status = "verified",
            }).ConfigureAwait(false);
    }

    public async Task<string> VerifyEvidencePacketAsync()
    {
        string[] required = ["complaints", "access", "asset", "sandstone", "constraints", "heritage", "survey", "cost"];
        string[] missing = required.Where(id => !HasEvidence(id)).ToArray();

        if (missing.Length > 0)
        {
            return await RecordToolOnlyAsync(new
            {
                caseId = "CW-2047",
                status = "incomplete",
                missingEvidence = missing,
                instruction = "Do not produce a recommendation. Ask the manager to obtain the missing evidence within the execution budget.",
            }).ConfigureAwait(false);
        }

        return await RecordEvidenceAsync(
            new EvidenceItemSnapshot(
                "verification",
                9,
                "EV-REPORT-01",
                "Evidence challenge completed",
                "Deterministic evidence-integrity check",
                "All material option claims have evidence references. Two matters remain explicitly unresolved: heritage significance and the final subsurface alignment.",
                EvidenceStatus.Verified,
                "Challenge complete",
                "Evidence Verifier",
                "7 claims linked; 2 matters unresolved"),
            new
            {
                caseId = "CW-2047",
                evidenceReference = "EV-REPORT-01",
                verified = true,
                supportedRecommendation = "C",
                requiredReferences = new[] { "SR-1182", "AR-ACCESS-12", "AR-DN-44", "OBS-07", "PC-22", "HR-CHECK-09", "SURVEY-04", "COST-IND-03" },
                challengedClaims = new[]
                {
                    "Option A does not resolve the mapped drainage conflict.",
                    "Option B is premature because excavation conditions are unverified.",
                    "Option C is the best-supported next step, not approval to perform works.",
                },
                openMatters = new[]
                {
                    "Whether the sandstone feature has heritage significance.",
                    "The verified subsurface drainage alignment.",
                },
                status = "verified_with_open_matters",
            }).ConfigureAwait(false);
    }

    private bool HasEvidence(string id)
    {
        if (!store.TryGetState(runId, out CivicWorksRunState? state) || state is null)
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            return state.Evidence.ContainsKey(id);
        }
    }

    private bool HasRevisedPlan()
    {
        if (!store.TryGetState(runId, out CivicWorksRunState? state) || state is null)
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            return state.PlanVersion > 1;
        }
    }

    private async Task<string> RecordEvidenceAsync(
        EvidenceItemSnapshot evidence,
        object result,
        string? focusEvidenceId = null)
    {
        CivicWorksRunSnapshot snapshot = store.Update(runId, state =>
        {
            EnsureToolBudget(state);
            state.ToolCallsUsed++;
            state.Evidence[evidence.Id] = evidence;
            state.ActiveSpecialist = evidence.Specialist;
            state.ActiveActivity = evidence.Activity;
            state.FocusEvidenceId = focusEvidenceId ?? evidence.Id;
            state.StatusMessage = evidence.Status == EvidenceStatus.Conflict
                ? "A material evidence conflict has invalidated the current route."
                : evidence.Activity;
        });

        await notifier.PublishAsync(snapshot.Id).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_jsonOptions);
    }

    private async Task<string> RecordToolOnlyAsync(object result)
    {
        CivicWorksRunSnapshot snapshot = store.Update(runId, state =>
        {
            EnsureToolBudget(state);
            state.ToolCallsUsed++;
        });

        await notifier.PublishAsync(snapshot.Id).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_jsonOptions);
    }

    private static void EnsureToolBudget(CivicWorksRunState state)
    {
        if (state.ToolCallsUsed >= CivicWorksRunState.MaximumToolCalls)
        {
            throw new InvalidOperationException(
                "The CivicWorks read-only tool-call budget is exhausted. No fallback result will be generated.");
        }
    }

}
