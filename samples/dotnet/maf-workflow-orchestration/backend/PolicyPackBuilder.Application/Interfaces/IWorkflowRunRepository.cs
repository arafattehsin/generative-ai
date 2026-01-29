// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Entities;
using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Application.Interfaces;

/// <summary>
/// Repository interface for WorkflowRun persistence operations.
/// </summary>
public interface IWorkflowRunRepository
{
    /// <summary>
    /// Gets a workflow run by ID.
    /// </summary>
    Task<WorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow run by ID with all step runs.
    /// </summary>
    Task<WorkflowRun?> GetByIdWithStepsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all workflow runs with pagination, ordered by creation date descending.
    /// </summary>
    Task<List<WorkflowRun>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflow runs by status.
    /// </summary>
    Task<List<WorkflowRun>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child re-runs of a parent run.
    /// </summary>
    Task<List<WorkflowRun>> GetChildRunsAsync(Guid parentRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runs in a lineage chain.
    /// </summary>
    Task<List<WorkflowRun>> GetLineageAsync(Guid rootRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of workflow runs.
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new workflow run.
    /// </summary>
    Task<WorkflowRun> CreateAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing workflow run.
    /// </summary>
    Task UpdateAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workflow run.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
