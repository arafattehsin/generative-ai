// Copyright (c) Microsoft. All rights reserved.

using OnboardFlow.Domain.Enums;

namespace OnboardFlow.Domain.Entities;

/// <summary>
/// Represents a single execution of the OnboardFlow workflow.
/// </summary>
public sealed class WorkflowRun
{
    public Guid Id { get; set; }

    /// <summary>
    /// If this run is a re-run, points to the immediate parent run.
    /// </summary>
    public Guid? ParentRunId { get; set; }

    /// <summary>
    /// Points to the original (root) run in the lineage chain.
    /// Null for original runs.
    /// </summary>
    public Guid? RootRunId { get; set; }

    public DateTime CreatedAt { get; set; }

    public WorkflowStatus Status { get; set; }

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

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? TotalDurationMs { get; set; }
    public string? Error { get; set; }

    /// <summary>
    /// The step name from which a re-run started (null for original runs).
    /// </summary>
    public string? RerunFromStep { get; set; }

    public WorkflowRun? ParentRun { get; set; }
    public ICollection<WorkflowRun> ChildRuns { get; set; } = [];
    public ICollection<WorkflowStepRun> Steps { get; set; } = [];
}
