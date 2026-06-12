// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using OnboardRoom.Application.Dtos;
using OnboardRoom.Domain.Entities;
using OnboardRoom.Domain.Enums;
using OnboardRoom.Domain.ValueObjects;

namespace OnboardRoom.Application.Services;

public static class RunMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static RunSummaryDto ToSummary(WorkflowRun run)
    {
        ChairRecommendation? recommendation = TryReadRecommendation(run);
        return new RunSummaryDto(
            run.Id,
            run.Status.ToString(),
            run.Region,
            run.Urgency,
            run.Tone,
            run.CreatedAt,
            run.CompletedAt,
            run.TotalDurationMs,
            run.ParentRunId,
            run.RootRunId,
            run.RerunFromStep,
            run.Steps.Count,
            run.Steps.Count(step => step.Status == StepStatus.Completed),
            recommendation?.Decision,
            recommendation?.RiskLevel,
            run.Error);
    }

    public static RunDetailDto ToDetail(WorkflowRun run) => new(
        run.Id,
        run.Status.ToString(),
        run.Region,
        run.Urgency,
        run.Tone,
        run.MaxRounds,
        run.CreatedAt,
        run.StartedAt,
        run.CompletedAt,
        run.TotalDurationMs,
        run.ParentRunId,
        run.RootRunId,
        run.RerunFromStep,
        run.InputTextRedacted,
        run.ProfileJson,
        run.ChairRecommendationJson,
        run.FinalOutputHtml,
        run.Error,
        run.Steps.OrderBy(step => step.StepOrder).Select(step => new StepDto(
            step.Id,
            step.StepName,
            step.StepOrder,
            step.Status.ToString(),
            step.StartedAt,
            step.CompletedAt,
            step.DurationMs,
            step.OutputSnapshot,
            step.Error)).ToList(),
        run.Messages.OrderBy(message => message.Sequence).Select(message => new GroupChatMessageDto(
            message.Id,
            message.Speaker,
            message.Role,
            message.Content,
            message.Sequence,
            message.CreatedAt)).ToList());

    private static ChairRecommendation? TryReadRecommendation(WorkflowRun run)
        => string.IsNullOrWhiteSpace(run.ChairRecommendationJson)
            ? null
            : JsonSerializer.Deserialize<ChairRecommendation>(run.ChairRecommendationJson, SerializerOptions);
}
