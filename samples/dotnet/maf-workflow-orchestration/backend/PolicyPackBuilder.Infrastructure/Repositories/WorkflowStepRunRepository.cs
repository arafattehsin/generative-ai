// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using PolicyPackBuilder.Application.Executors;
using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Domain.Entities;
using PolicyPackBuilder.Domain.Enums;
using PolicyPackBuilder.Infrastructure.Data;

namespace PolicyPackBuilder.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for WorkflowStepRun persistence.
/// </summary>
public sealed class WorkflowStepRunRepository : IWorkflowStepRunRepository
{
    private readonly PolicyPackDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the WorkflowStepRunRepository.
    /// </summary>
    public WorkflowStepRunRepository(PolicyPackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<WorkflowStepRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<WorkflowStepRun>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns
            .Where(s => s.RunId == runId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowStepRun?> GetByRunIdAndStepNameAsync(Guid runId, string stepName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns
            .FirstOrDefaultAsync(s => s.RunId == runId && s.StepName == stepName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowStepRun>> GetCompletedStepsBeforeAsync(
        Guid runId,
        string stepName,
        CancellationToken cancellationToken = default)
    {
        int stepOrder = WorkflowSteps.GetStepOrder(stepName);
        return await GetCompletedStepsBeforeAsync(runId, stepOrder, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowStepRun>> GetCompletedStepsBeforeAsync(
        Guid runId,
        int stepOrder,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns
            .Where(s => s.RunId == runId && s.StepOrder < stepOrder && s.Status == StepStatus.Completed)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowStepRun?> GetLastCompletedStepAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns
            .Where(s => s.RunId == runId && s.Status == StepStatus.Completed)
            .OrderByDescending(s => s.StepOrder)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowStepRun> CreateAsync(WorkflowStepRun stepRun, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowStepRuns.Add(stepRun);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return stepRun;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(WorkflowStepRun stepRun, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowStepRuns.Update(stepRun);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
