// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Entities;

namespace PolicyPackBuilder.Application.Orchestration;

/// <summary>
/// Interface for orchestrating policy pack workflow execution.
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Starts a new workflow run with the given input and options.
    /// </summary>
    /// <param name="inputText">The raw policy text to process.</param>
    /// <param name="optionsJson">The workflow options as JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created workflow run.</returns>
    Task<WorkflowRun> StartRunAsync(string inputText, string optionsJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-runs a workflow from a specific step, creating a new run with lineage.
    /// </summary>
    /// <param name="parentRunId">The parent run ID to re-run from.</param>
    /// <param name="fromStep">The step to re-run from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new workflow run.</returns>
    Task<WorkflowRun> RerunFromStepAsync(Guid parentRunId, string fromStep, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow.
    /// </summary>
    /// <param name="runId">The run ID to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelRunAsync(Guid runId, CancellationToken cancellationToken = default);
}
