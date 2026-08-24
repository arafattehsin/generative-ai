using System.Collections.Concurrent;
using CivicWorks.Api.Models;

namespace CivicWorks.Api.Services;

public sealed class CivicWorksRunStore
{
    private readonly ConcurrentDictionary<Guid, CivicWorksRunState> _runs = new();

    public CivicWorksRunSnapshot Create()
    {
        CivicWorksRunState state = new();
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
            update(state);
            state.UpdatedAt = DateTimeOffset.UtcNow;
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

    public CivicWorksRunSnapshot ToSnapshot()
    {
        int elapsed = Math.Min(
            MaximumElapsedSeconds,
            Math.Max(0, (int)(DateTimeOffset.UtcNow - StartedAt).TotalSeconds));

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
            Error);
    }
}
