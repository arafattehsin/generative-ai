// Copyright (c) Microsoft. All rights reserved.

using OnboardRoom.Domain.Enums;

namespace OnboardRoom.Domain.Entities;

public sealed class WorkflowRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public RunStatus Status { get; set; } = RunStatus.Pending;

    public string Region { get; set; } = "AU";

    public string Urgency { get; set; } = "Normal";

    public string Tone { get; set; } = "Executive";

    public int MaxRounds { get; set; } = 6;

    public string InputTextOriginal { get; set; } = string.Empty;

    public string InputTextRedacted { get; set; } = string.Empty;

    public string? ProfileJson { get; set; }

    public string? ChairRecommendationJson { get; set; }

    public string? FinalOutputHtml { get; set; }

    public string? Error { get; set; }

    public Guid? ParentRunId { get; set; }

    public Guid? RootRunId { get; set; }

    public string? RerunFromStep { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public long? TotalDurationMs => this.StartedAt.HasValue && this.CompletedAt.HasValue
        ? (long)(this.CompletedAt.Value - this.StartedAt.Value).TotalMilliseconds
        : null;

    public List<WorkflowStepRun> Steps { get; set; } = [];

    public List<GroupChatMessage> Messages { get; set; } = [];
}
