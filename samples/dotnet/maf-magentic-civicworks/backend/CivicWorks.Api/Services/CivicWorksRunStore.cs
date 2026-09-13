using System.Collections.Concurrent;
using CivicWorks.Api.Models;

namespace CivicWorks.Api.Services;

public sealed class CivicWorksRunStore
{
    private readonly ConcurrentDictionary<Guid, CivicWorksRunState> _runs = new();

    public CivicWorksRunSnapshot Create()
    {
        CivicWorksRunState state = new();
        state.Activity.Add(new RunActivity(
            1, state.StartedAt, "started", "CivicWorks",
            "Investigation opened", state.StatusMessage, null, 0));
        if (!_runs.TryAdd(state.Id, state))
        {
            throw new InvalidOperationException("Could not allocate a CivicWorks run.");
        }

        return state.ToSnapshot();
    }

    public bool TryGet(Guid id, out CivicWorksRunSnapshot? snapshot)
    {
        if (!_runs.TryGetValue(id, out CivicWorksRunState? state))
        {
            snapshot = null;
            return false;
        }

        lock (state.SyncRoot)
        {
            snapshot = state.ToSnapshot();
            return true;
        }
    }

    internal CivicWorksRunSnapshot Update(Guid id, Action<CivicWorksRunState> update)
    {
        if (!_runs.TryGetValue(id, out CivicWorksRunState? state))
        {
            throw new KeyNotFoundException($"CivicWorks run '{id}' was not found.");
        }

        lock (state.SyncRoot)
        {
            string previousStatus = state.StatusMessage;
            string? previousActor = state.ActiveSpecialist;
            string? previousActivity = state.ActiveActivity;
            int previousToolCalls = state.ToolCallsUsed;
            int previousRounds = state.RoundsUsed;
            RunPhase previousPhase = state.Phase;
            update(state);
            state.UpdatedAt = DateTimeOffset.UtcNow;

            if (state.PlanVersion > 0 && !string.IsNullOrWhiteSpace(state.PlanText))
            {
                int index = state.Plans.FindIndex(plan => plan.Version == state.PlanVersion);
                if (index < 0)
                {
                    state.Plans.Add(new InvestigationPlan(
                        state.PlanVersion, state.PlanText, state.UpdatedAt, null, []));
                }
                else if (state.Plans[index].ApprovedAt is null)
                {
                    state.Plans[index] = state.Plans[index] with { Text = state.PlanText };
                }
            }

            bool toolCalled = state.ToolCallsUsed != previousToolCalls;
            if (state.StatusMessage != previousStatus || state.Phase != previousPhase ||
                state.ActiveSpecialist != previousActor || state.ActiveActivity != previousActivity ||
                toolCalled || state.RoundsUsed != previousRounds)
            {
                EvidenceItemSnapshot? evidence = toolCalled && state.FocusEvidenceId is not null &&
                    (state.StatusMessage != previousStatus || state.ActiveActivity != previousActivity)
                    ? state.Evidence.GetValueOrDefault(state.FocusEvidenceId)
                    : null;
                string kind = state.Phase switch
                {
                    RunPhase.InitialReview or RunPhase.RevisedReview => "review",
                    RunPhase.Completed => "completed",
                    RunPhase.Failed or RunPhase.BoundedIncomplete => "stopped",
                    _ when evidence?.Status == EvidenceStatus.Conflict => "conflict",
                    _ when toolCalled => "evidence",
                    _ when state.RoundsUsed != previousRounds => "delegation",
                    _ when state.Phase == RunPhase.Planning && state.PlanVersion > 1 => "replan",
                    _ => "activity",
                };
                state.Activity.Add(new RunActivity(
                    state.Activity[^1].Sequence + 1, state.UpdatedAt, kind,
                    state.ActiveSpecialist ?? (kind == "review" ? "Officer checkpoint" : "CivicWorks"),
                    evidence?.Title ?? (toolCalled ? "Read-only tool call completed" : state.StatusMessage != previousStatus || state.Phase != previousPhase
                        ? state.StatusMessage : state.ActiveActivity ?? state.StatusMessage),
                    evidence?.Detail ?? (toolCalled ? "The tool returned without adding a new evidence record." : state.ActiveActivity),
                    evidence?.Id, state.PlanVersion));
                // Keep the in-memory demo and each SignalR update bounded.
                if (state.Activity.Count > 240) state.Activity.RemoveAt(0);
            }
            return state.ToSnapshot();
        }
    }

    internal bool TryGetState(Guid id, out CivicWorksRunState? state) => _runs.TryGetValue(id, out state);
}

internal sealed class CivicWorksRunState
{
    public const int MaximumRounds = 12;
    public const int MaximumToolCalls = 24;
    public const int MaximumStallsBeforeReplan = 0;
    public const int MaximumResets = 1;
    public const int MaximumElapsedSeconds = 600;

    public object SyncRoot { get; } = new();
    public Guid Id { get; } = Guid.NewGuid();
    public string CaseId { get; } = "CW-2047";
    public string CaseName { get; } = "Marrin Precinct";
    public RunPhase Phase { get; set; } = RunPhase.Starting;
    public string StatusMessage { get; set; } = "Preparing a live Microsoft Foundry investigation.";
    public int PlanVersion { get; set; }
    public string? PlanText { get; set; }
    public string? RevisionReason { get; set; }
    public string? ActiveSpecialist { get; set; }
    public string? ActiveActivity { get; set; }
    public string? FocusEvidenceId { get; set; }
    public Dictionary<string, EvidenceItemSnapshot> Evidence { get; } = new(StringComparer.Ordinal);
    public int RoundsUsed { get; set; }
    public int ToolCallsUsed { get; set; }
    public int StallsObserved { get; set; }
    public int ResetsUsed { get; set; }
    public PreliminaryWorksOptionsBrief? Brief { get; set; }
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Error { get; set; }
    public List<RunActivity> Activity { get; } = [];
    public List<InvestigationPlan> Plans { get; } = [];

    public CivicWorksRunSnapshot ToSnapshot()
    {
        int elapsed = Math.Min(
            MaximumElapsedSeconds,
            Math.Max(0, (int)((Phase is RunPhase.Completed or RunPhase.Failed or RunPhase.BoundedIncomplete
                ? UpdatedAt : DateTimeOffset.UtcNow) - StartedAt).TotalSeconds));

        return new CivicWorksRunSnapshot(
            Id,
            CaseId,
            CaseName,
            Phase,
            StatusMessage,
            PlanVersion,
            PlanText,
            RevisionReason,
            ActiveSpecialist,
            ActiveActivity,
            FocusEvidenceId,
            Evidence.Values.OrderBy(item => item.Sequence).ToArray(),
            new ExecutionBudgetSnapshot(
                RoundsUsed,
                MaximumRounds,
                ToolCallsUsed,
                MaximumToolCalls,
                StallsObserved,
                MaximumStallsBeforeReplan,
                ResetsUsed,
                MaximumResets,
                elapsed,
                MaximumElapsedSeconds),
            Brief,
            StartedAt,
            UpdatedAt,
            Error,
            Activity.ToArray(),
            Plans.Select(plan => plan with { Constraints = plan.Constraints.ToArray() }).ToArray());
    }
}
