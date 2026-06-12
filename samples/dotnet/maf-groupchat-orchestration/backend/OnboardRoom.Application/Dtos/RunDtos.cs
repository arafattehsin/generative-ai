// Copyright (c) Microsoft. All rights reserved.

namespace OnboardRoom.Application.Dtos;

public sealed record StartRunRequest(
    string InputText,
    string Region = "AU",
    string Urgency = "Normal",
    int MaxRounds = 6,
    string Tone = "Executive");

public sealed record StartRunResponse(Guid RunId, string Status, DateTime CreatedAt);

public sealed record RerunRequest(string FromStep);

public sealed record RerunResponse(Guid NewRunId, Guid ParentRunId, string FromStep, string Status, DateTime CreatedAt);

public sealed record RunSummaryDto(
    Guid Id,
    string Status,
    string Region,
    string Urgency,
    string Tone,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    long? TotalDurationMs,
    Guid? ParentRunId,
    Guid? RootRunId,
    string? RerunFromStep,
    int StepCount,
    int CompletedStepCount,
    string? RecommendationDecision,
    string? RecommendationRisk,
    string? Error);

public sealed record RunDetailDto(
    Guid Id,
    string Status,
    string Region,
    string Urgency,
    string Tone,
    int MaxRounds,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    long? TotalDurationMs,
    Guid? ParentRunId,
    Guid? RootRunId,
    string? RerunFromStep,
    string InputTextRedacted,
    string? ProfileJson,
    string? ChairRecommendationJson,
    string? FinalOutputHtml,
    string? Error,
    IReadOnlyList<StepDto> Steps,
    IReadOnlyList<GroupChatMessageDto> Messages);

public sealed record StepDto(
    Guid Id,
    string StepName,
    int StepOrder,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    long? DurationMs,
    string? OutputSnapshot,
    string? Error);

public sealed record GroupChatMessageDto(
    Guid Id,
    string Speaker,
    string Role,
    string Content,
    int Sequence,
    DateTime CreatedAt);

public sealed record StepDefinitionDto(string Name, int Order, string Description, bool IsGroupChatStep);

public sealed record SampleRequestDto(string Title, string Region, string Urgency, string Text);

public sealed record WorkflowEventDto(
    Guid RunId,
    string Kind,
    string? StepName,
    string? Speaker,
    string? Content,
    DateTime Timestamp);
