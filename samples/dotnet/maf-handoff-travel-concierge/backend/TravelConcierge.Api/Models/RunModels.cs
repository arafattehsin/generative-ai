using System.Text.Json.Serialization;

namespace TravelConcierge.Api.Models;

public enum RunStatus
{
    Queued,
    Running,
    WaitingForUser,
    WaitingForHuman,
    HumanResponding,
    Failed,
    Cancelled,
}

public enum ConversationAuthor
{
    User,
    Agent,
    Human,
    System,
}

public enum HumanSupportStatus
{
    Requested,
    Claimed,
    Resolved,
}

public sealed record ConfigResponse(
    bool HasProjectEndpoint,
    string ProjectEndpoint,
    string DeploymentName,
    IReadOnlyList<AgentDefinition> Agents);

public sealed record CreateRunRequest(
    string Message,
    string TravellerName,
    string TripCode,
    string Urgency);

public sealed record SendMessageRequest(string Message);

public sealed record ClaimHumanSupportRequest(string OperatorName);

public sealed record ResolveHumanSupportRequest(
    string OperatorName,
    string Message,
    string NextOwnerId);

public sealed record RunCreatedResponse(Guid RunId);

public sealed record SampleScenario(
    string Id,
    string Title,
    string TravellerName,
    string TripCode,
    string Urgency,
    string Message,
    IReadOnlyList<string> Tags);

public sealed class TravelRun
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public RunStatus Status { get; set; } = RunStatus.Queued;
    public string Mode { get; init; } = "foundry";
    public string TravellerName { get; init; } = string.Empty;
    public string TripCode { get; init; } = string.Empty;
    public string Urgency { get; init; } = "High";
    public string InitialRequest { get; init; } = string.Empty;
    public string CurrentOwnerId { get; set; } = TravelAgents.Triage.Id;
    public string CurrentOwnerName { get; set; } = TravelAgents.Triage.Name;
    public string Summary { get; set; } = "Waiting for first response.";
    public TravelCaseSnapshot Case { get; set; } = TravelCaseSnapshot.Empty;
    public List<ConversationMessage> Messages { get; } = [];
    public List<HandoffTimelineItem> Timeline { get; } = [];
    public IReadOnlyList<AgentDefinition> Agents { get; init; } = TravelAgents.All;
    public HumanSupportSnapshot? HumanSupport { get; set; }

    [JsonIgnore]
    public object SyncRoot { get; } = new();
}

public sealed record HumanSupportRequest(
    Guid RunId,
    string TravellerName,
    string TripCode,
    string Urgency,
    string RequestedByAgentId,
    string RequestedByAgentName,
    string Reason,
    string DecisionNeeded,
    string Recommendation,
    string ConversationSummary);

public sealed record HumanSupportResolution(
    string OperatorName,
    string Message,
    string NextOwnerId);

public sealed class HumanSupportSnapshot
{
    public string RequestId { get; init; } = string.Empty;
    public HumanSupportStatus Status { get; set; } = HumanSupportStatus.Requested;
    public string RequestedByAgentId { get; init; } = string.Empty;
    public string RequestedByAgentName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string DecisionNeeded { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public string ConversationSummary { get; init; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed class ConversationMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ConversationAuthor AuthorType { get; init; }
    public string? AgentId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record HandoffTimelineItem(
    Guid Id,
    string FromAgentId,
    string FromAgentName,
    string ToAgentId,
    string ToAgentName,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record TravelCaseSnapshot(
    string Route,
    string Disruption,
    string Constraint,
    string RecoveryPlan,
    string EvidenceNeeded,
    string Risk)
{
    public static TravelCaseSnapshot Empty { get; } = new(
        "Not captured",
        "Not classified",
        "Unknown",
        "No recovery plan yet",
        "No evidence list yet",
        "Not assessed");
}
