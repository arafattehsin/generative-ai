// Copyright (c) Microsoft. All rights reserved.

using OnboardFlow.Domain.Enums;

namespace OnboardFlow.Domain.Entities;

/// <summary>
/// Represents the execution of a single step within a workflow run.
/// </summary>
public sealed class WorkflowStepRun
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public StepStatus Status { get; set; }

    /// <summary>
    /// Groups concurrent steps together (e.g. "ConcurrentReview").
    /// Null for sequential steps.
    /// </summary>
    public string? ConcurrencyGroup { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }

    public string? InputSnapshot { get; set; }
    public bool InputIsTruncated { get; set; }
    public int? InputFullLength { get; set; }

    public string? OutputSnapshot { get; set; }
    public bool OutputIsTruncated { get; set; }
    public int? OutputFullLength { get; set; }

    public string? WarningsJson { get; set; }
    public string? Error { get; set; }

    public WorkflowRun Run { get; set; } = null!;
}
