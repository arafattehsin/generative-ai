namespace IncidentCommandCenter.Api.Models;

public sealed record IncidentRecord(
    string Id,
    string Title,
    string Region,
    string Vendor,
    string Status,
    int OpenOrders,
    int AtRiskOrders,
    string SlaDeadlineUtc,
    string Summary,
    IReadOnlyList<string> Signals,
    IReadOnlyList<string> Constraints,
    string LastUpdatedUtc
);

public sealed record SessionResponse(string SessionId);

public sealed record ErrorResponse(string Error, string? Detail = null);

public sealed record TriageRequest(
    string SessionId,
    string IncidentId,
    string? UserPrompt
);

public sealed record TriageResponse(
    string RunId,
    string Summary,
    string Severity,
    IReadOnlyList<string> ProbableCauses,
    IReadOnlyList<string> RecommendedActions,
    IReadOnlyList<string> AtRiskOrders
);

public sealed record CommunicationDraftRequest(
    string SessionId,
    string IncidentId,
    string Audience,
    string? TriageSummary
);

public sealed record CommunicationDraftResponse(
    string RunId,
    string Audience,
    string Draft
);

public sealed record SkillSummary(
    string Name,
    string Description,
    IReadOnlyList<string> Resources
);

public sealed record SkillEvent(
    string RunId,
    string Timestamp,
    string Stage,
    string SkillName,
    string? Resource,
    string? Note
);

public sealed record TriageModel(
    string Summary,
    string Severity,
    IReadOnlyList<string> ProbableCauses,
    IReadOnlyList<string> RecommendedActions,
    IReadOnlyList<string> AtRiskOrders
);
