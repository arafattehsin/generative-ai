// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Entities;

namespace PolicyPackBuilder.Application.Interfaces;

/// <summary>
/// Repository interface for WorkflowStepRun persistence operations.
/// </summary>
public interface IWorkflowStepRunRepository
{
    /// <summary>
    /// Gets a step run by ID.
    /// </summary>
    Task<WorkflowStepRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all step runs for a workflow run, ordered by step order.
    /// </summary>
    Task<List<WorkflowStepRun>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a step run by run ID and step name.
    /// </summary>
    Task<WorkflowStepRun?> GetByRunIdAndStepNameAsync(Guid runId, string stepName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets completed step runs up to (but not including) the specified step name.
    /// Used for re-run functionality to reconstruct workflow context.
    /// </summary>
    Task<IReadOnlyList<WorkflowStepRun>> GetCompletedStepsBeforeAsync(
        Guid runId,
        string stepName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets completed step runs up to (but not including) the specified step order.
    /// Used for re-run functionality to reconstruct workflow context.
    /// </summary>
    Task<IReadOnlyList<WorkflowStepRun>> GetCompletedStepsBeforeAsync(
        Guid runId,
        int stepOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last completed step for a run.
    /// </summary>
    Task<WorkflowStepRun?> GetLastCompletedStepAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new step run.
    /// </summary>
    Task<WorkflowStepRun> CreateAsync(WorkflowStepRun stepRun, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing step run.
    /// </summary>
    Task UpdateAsync(WorkflowStepRun stepRun, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
