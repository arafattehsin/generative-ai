using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CivicWorks.Api.Services;

internal sealed class EvidenceSnapshotContextProvider(
    Guid runId,
    CivicWorksRunStore store) : MessageAIContextProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> ProvideMessagesAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        if (!store.TryGetState(runId, out CivicWorksRunState? state) || state is null)
        {
            return ValueTask.FromResult<IEnumerable<ChatMessage>>([]);
        }

        return ValueTask.FromResult<IEnumerable<ChatMessage>>(
            [new ChatMessage(ChatRole.User, BuildSnapshot(state))]);
    }

    internal static string BuildSnapshot(CivicWorksRunState state)
    {
        StringBuilder snapshot = new();
        lock (state.SyncRoot)
        {
            snapshot.AppendLine("CIVICWORKS LIVE EVIDENCE SNAPSHOT (application supplied; authoritative for what tools have actually run)");
            snapshot.AppendLine($"Current application plan version: {state.PlanVersion}");
            snapshot.AppendLine($"Tool calls used: {state.ToolCallsUsed}/{CivicWorksRunState.MaximumToolCalls}");

            if (state.Evidence.Count == 0)
            {
                snapshot.AppendLine("Collected evidence: none.");
            }
            else
            {
                snapshot.AppendLine("Collected evidence:");
                foreach (var item in state.Evidence.Values.OrderBy(item => item.Sequence))
                {
                    snapshot.AppendLine($"- {item.Reference} [{item.Status}]: {item.Detail}");
                }
            }

            bool hasAsset = state.Evidence.ContainsKey("asset");
            bool hasObservation = state.Evidence.ContainsKey("sandstone");
            string[] missingCommunity = new[] { "complaints", "access" }
                .Where(id => !state.Evidence.ContainsKey(id)).ToArray();
            bool revised = state.PlanVersion > 1;

            snapshot.AppendLine("Manager guard:");
            snapshot.AppendLine($"- Tool-backed AR-DN-44 present: {hasAsset}.");
            snapshot.AppendLine($"- Tool-backed OBS-07 present: {hasObservation}.");
            snapshot.AppendLine($"- A revised plan already exists: {revised}.");

            if (revised)
            {
                snapshot.AppendLine("- Only the evidence listed above survived the reset. Do not repeat tools for records already present; never assume an unlisted record was collected.");
                if (missingCommunity.Length > 0)
                {
                    snapshot.AppendLine($"- Missing baseline evidence: {string.Join(", ", missingCommunity)}. Select CommunityAccessAnalyst to obtain these missing records before the final EvidenceVerifier challenge.");
                }
                if (!hasAsset)
                {
                    snapshot.AppendLine("- Missing baseline evidence: asset. Select CivilAssetsAnalyst to obtain AR-DN-44 before the final EvidenceVerifier challenge.");
                }
                snapshot.AppendLine("- Do not trigger another reset from historical AR-DN-44/OBS-07 conflict text. Complete any missing baseline evidence, then missing revised-plan checks: Place & Constraints, Cost & Delivery, Evidence Verifier.");
                snapshot.AppendLine("- Mark is_progress_being_made=true while those missing revised-plan checks are being completed.");
            }
            else if (hasAsset && hasObservation)
            {
                snapshot.AppendLine("- The tool-backed conflict threshold is satisfied. The original direct-works route must be reset exactly once.");
            }
            else
            {
                snapshot.AppendLine("- The evidence threshold for replanning is not satisfied. Follow the initial collection order; the conflict check does not replace the baseline service-request and access investigation.");
                if (missingCommunity.Length > 0)
                {
                    snapshot.AppendLine($"- Missing baseline evidence: {string.Join(", ", missingCommunity)}. Select CommunityAccessAnalyst before collecting civil assets and site observations.");
                }
                else
                {
                    snapshot.AppendLine("- Baseline service-request and access evidence is present. Select the specialist needed to collect the missing asset or site-observation reference.");
                }
            }

            string[] missingForVerification = new[] { "complaints", "access", "asset", "sandstone", "constraints", "heritage", "survey", "cost" }
                .Where(id => !state.Evidence.ContainsKey(id)).ToArray();
            if (missingForVerification.Length > 0)
            {
                snapshot.AppendLine($"- EvidenceVerifier completion prerequisites still missing: {string.Join(", ", missingForVerification)}. Obtain them before asking the verifier to complete the packet. Revised-plan checks still require the revised plan's approval.");
            }
            else if (state.Evidence.ContainsKey("verification"))
            {
                snapshot.AppendLine("- All required specialist work is complete, including the tool-backed EV-REPORT-01. No further verifier readiness confirmation or assembly handoff is needed.");
                snapshot.AppendLine("- In the next progress ledger, mark is_request_satisfied=true. This tells the Magentic framework to run final-answer synthesis; it does not claim a final JSON has already been written. During that synthesis, produce the evidence-cited FINAL OUTPUT CONTRACT and retain the unresolved matters.");
            }
            else
            {
                snapshot.AppendLine("- All verification prerequisites are present. Require EvidenceVerifier to run its tool; EV-REPORT-01 is not yet in the collected evidence.");
            }
        }

        return snapshot.ToString();
    }
}
