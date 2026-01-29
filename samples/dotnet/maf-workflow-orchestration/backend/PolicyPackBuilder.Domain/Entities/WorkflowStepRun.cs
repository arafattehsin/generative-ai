// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Domain.Entities;

/// <summary>
/// Represents the execution of a single step within a workflow run.
/// </summary>
public sealed class WorkflowStepRun
{
    /// <summary>
    /// Unique identifier for this step run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the parent workflow run.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Name of the workflow step (e.g., "IntakeNormalize", "ExtractFacts").
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Order of this step in the workflow (1-based).
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Current status of the step.
    /// </summary>
    public StepStatus Status { get; set; }

    /// <summary>
    /// When the step started executing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the step completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration of the step execution in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// JSON snapshot of the step input (may be truncated).
    /// </summary>
    public string? InputSnapshot { get; set; }

    /// <summary>
    /// Whether the input snapshot was truncated.
    /// </summary>
    public bool InputIsTruncated { get; set; }

    /// <summary>
    /// Full length of input before truncation.
    /// </summary>
    public int? InputFullLength { get; set; }

    /// <summary>
    /// JSON snapshot of the step output (may be truncated).
    /// </summary>
    public string? OutputSnapshot { get; set; }

    /// <summary>
    /// Whether the output snapshot was truncated.
    /// </summary>
    public bool OutputIsTruncated { get; set; }

    /// <summary>
    /// Full length of output before truncation.
    /// </summary>
    public int? OutputFullLength { get; set; }

    /// <summary>
    /// JSON array of warnings generated during step execution.
    /// </summary>
    public string? WarningsJson { get; set; }

    /// <summary>
    /// Error message if the step failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Navigation property to parent workflow run.
    /// </summary>
    public WorkflowRun Run { get; set; } = null!;
}
