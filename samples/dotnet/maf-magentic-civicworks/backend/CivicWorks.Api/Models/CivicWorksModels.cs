using System.Text.Json.Serialization;

namespace CivicWorks.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<RunPhase>))]
public enum RunPhase
{
    Starting,
    Planning,
    InitialReview,
    Running,
    RevisedReview,
    Completed,
    BoundedIncomplete,
    Failed,
}

[JsonConverter(typeof(JsonStringEnumConverter<EvidenceStatus>))]
public enum EvidenceStatus
{
    Queued,
    Checking,
    Verified,
    Conflict,
}

public sealed record EvidenceItemSnapshot(
    string Id,
    int Sequence,
    string Reference,
    string Title,
    string Source,
    string Detail,
    EvidenceStatus Status,
    string Updated,
    string Specialist,
    string Activity);

public sealed record ExecutionBudgetSnapshot(
    int RoundsUsed,
    int MaxRounds,
    int ToolCallsUsed,
    int MaxToolCalls,
    int StallsObserved,
    int MaxStallsBeforeReplan,
    int ResetsUsed,
    int MaxResets,
    int ElapsedSeconds,
    int MaxElapsedSeconds);

public sealed record WorksOption(
    string Code,
    string Title,
    string Assessment,
    string SupportStatus);

public sealed record EvidenceClaim(
    string Claim,
    IReadOnlyList<string> EvidenceReferences);

public sealed record PreliminaryWorksOptionsBrief(
    string Recommendation,
    string Summary,
    IReadOnlyList<WorksOption> Options,
    IReadOnlyList<EvidenceClaim> Claims,
    IReadOnlyList<string> OpenMatters);

public sealed record CivicWorksRunSnapshot(
    Guid Id,
    string CaseId,
    string CaseName,
    RunPhase Phase,
    string StatusMessage,
    int PlanVersion,
    string? PlanText,
    string? RevisionReason,
    string? ActiveSpecialist,
    string? ActiveActivity,
    string? FocusEvidenceId,
    IReadOnlyList<EvidenceItemSnapshot> Evidence,
    ExecutionBudgetSnapshot Budget,
    PreliminaryWorksOptionsBrief? Brief,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    string? Error);

public sealed record PlanReviewCommand(
    string Action,
    string? Feedback,
    IReadOnlyList<string>? Constraints);

public sealed record FoundryConfigurationStatus(
    bool IsConfigured,
    bool SimulationFallbackEnabled,
    string Authentication,
    string? ModelDeployment,
    IReadOnlyList<string> MissingSettings);
