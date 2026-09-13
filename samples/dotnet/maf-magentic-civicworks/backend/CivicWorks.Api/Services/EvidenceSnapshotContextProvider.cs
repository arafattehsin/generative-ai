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
            bool revised = state.PlanVersion > 1;

            snapshot.AppendLine("Manager guard:");
            snapshot.AppendLine($"- Tool-backed AR-DN-44 present: {hasAsset}.");
            snapshot.AppendLine($"- Tool-backed OBS-07 present: {hasObservation}.");
            snapshot.AppendLine($"- A revised plan already exists: {revised}.");

            if (revised)
            {
                snapshot.AppendLine("- Do not repeat Community & Access or Civil Assets collection. Those facts survived the reset in this snapshot.");
                snapshot.AppendLine("- Do not trigger another reset from AR-DN-44/OBS-07. Continue with missing revised-plan evidence in this order: Place & Constraints, Cost & Delivery, Evidence Verifier.");
                snapshot.AppendLine("- Mark is_progress_being_made=true while those missing revised-plan checks are being completed.");
            }
            else if (hasAsset && hasObservation)
            {
                snapshot.AppendLine("- The tool-backed conflict threshold is satisfied. The original direct-works route must be reset exactly once.");
            }
            else
            {
                snapshot.AppendLine("- The evidence threshold for replanning is not satisfied. Select the specialist needed to collect the missing reference.");
            }
        }

        return ValueTask.FromResult<IEnumerable<ChatMessage>>(
            [new ChatMessage(ChatRole.User, snapshot.ToString())]);
    }
}
