// Copyright (c) Microsoft. All rights reserved.

using OnboardRoom.Domain.Enums;

namespace OnboardRoom.Domain.Entities;

public sealed class WorkflowStepRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RunId { get; set; }

    public WorkflowRun? Run { get; set; }

    public string StepName { get; set; } = string.Empty;

    public int StepOrder { get; set; }

    public StepStatus Status { get; set; } = StepStatus.Pending;

    public string? InputSnapshot { get; set; }

    public string? OutputSnapshot { get; set; }

    public string? Error { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public long? DurationMs => this.StartedAt.HasValue && this.CompletedAt.HasValue
        ? (long)(this.CompletedAt.Value - this.StartedAt.Value).TotalMilliseconds
        : null;
}
