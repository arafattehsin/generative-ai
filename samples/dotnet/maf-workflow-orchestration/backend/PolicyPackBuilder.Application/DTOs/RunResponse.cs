// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Application.DTOs;

/// <summary>
/// Response containing workflow run details.
/// </summary>
public sealed class RunResponse
{
    /// <summary>
    /// Unique identifier for the run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Parent run ID if this is a re-run.
    /// </summary>
    public Guid? ParentRunId { get; set; }

    /// <summary>
    /// Root run ID for lineage tracking.
    /// </summary>
    public Guid? RootRunId { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the run was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the run started executing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the run completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total duration in milliseconds.
    /// </summary>
    public long? TotalDurationMs { get; set; }

    /// <summary>
    /// Redacted input text.
    /// </summary>
    public string? InputTextRedacted { get; set; }

    /// <summary>
    /// Final HTML output (if completed).
    /// </summary>
    public string? FinalOutputHtml { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Step name from which a re-run started.
    /// </summary>
    public string? RerunFromStep { get; set; }

    /// <summary>
    /// Workflow options.
    /// </summary>
    public RunOptionsDto? Options { get; set; }

    /// <summary>
    /// List of step runs.
    /// </summary>
    public List<StepResponse> Steps { get; set; } = [];
}

/// <summary>
/// Response for creating a run.
/// </summary>
public sealed class CreateRunResponse
{
    /// <summary>
    /// The created run ID.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the run was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response for re-running a workflow.
/// </summary>
public sealed class RerunResponse
{
    /// <summary>
    /// The new run ID.
    /// </summary>
    public Guid NewRunId { get; set; }

    /// <summary>
    /// Parent run ID.
    /// </summary>
    public Guid ParentRunId { get; set; }

    /// <summary>
    /// Step from which the re-run starts.
    /// </summary>
    public string FromStep { get; set; } = string.Empty;

    /// <summary>
    /// Current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Summary of a run for list views.
/// </summary>
public sealed class RunSummaryResponse
{
    /// <summary>
    /// Run ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Parent run ID if this is a re-run.
    /// </summary>
    public Guid? ParentRunId { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total duration in milliseconds.
    /// </summary>
    public long? TotalDurationMs { get; set; }

    /// <summary>
    /// Step from which a re-run started.
    /// </summary>
    public string? RerunFromStep { get; set; }

    /// <summary>
    /// Number of completed steps.
    /// </summary>
    public int CompletedSteps { get; set; }

    /// <summary>
    /// Total number of steps.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Workflow options.
    /// </summary>
    public RunOptionsDto? Options { get; set; }
}