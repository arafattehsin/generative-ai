// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Domain.Entities;

/// <summary>
/// Represents a single execution of the PolicyPack workflow.
/// </summary>
public sealed class WorkflowRun
{
    /// <summary>
    /// Unique identifier for this workflow run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// If this run is a re-run, points to the immediate parent run.
    /// </summary>
    public Guid? ParentRunId { get; set; }

    /// <summary>
    /// Points to the original (root) run in the lineage chain.
    /// Null for original runs, set for all re-runs.
    /// </summary>
    public Guid? RootRunId { get; set; }

    /// <summary>
    /// When the run was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Current status of the workflow run.
    /// </summary>
    public WorkflowStatus Status { get; set; }

    /// <summary>
    /// JSON-serialized workflow options.
    /// </summary>
    public string OptionsJson { get; set; } = string.Empty;

    /// <summary>
    /// Original input text before any processing or redaction.
    /// </summary>
    public string InputTextOriginal { get; set; } = string.Empty;

    /// <summary>
    /// Input text with PII redacted (used for LLM processing).
    /// </summary>
    public string InputTextRedacted { get; set; } = string.Empty;

    /// <summary>
    /// Final HTML output of the workflow, if completed successfully.
    /// </summary>
    public string? FinalOutputHtml { get; set; }

    /// <summary>
    /// When the workflow started executing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the workflow completed (success, failure, or cancellation).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total duration of the workflow run in milliseconds.
    /// </summary>
    public long? TotalDurationMs { get; set; }

    /// <summary>
    /// Error message if the workflow failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The step name from which a re-run started (null for original runs).
    /// </summary>
    public string? RerunFromStep { get; set; }

    /// <summary>
    /// Navigation property to parent run.
    /// </summary>
    public WorkflowRun? ParentRun { get; set; }

    /// <summary>
    /// Navigation property to child re-runs.
    /// </summary>
    public ICollection<WorkflowRun> ChildRuns { get; set; } = [];

    /// <summary>
    /// Collection of step runs for this workflow.
    /// </summary>
    public ICollection<WorkflowStepRun> Steps { get; set; } = [];
}
