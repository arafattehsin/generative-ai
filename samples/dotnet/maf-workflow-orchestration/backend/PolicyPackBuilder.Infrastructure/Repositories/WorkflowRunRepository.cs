// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Domain.Entities;
using PolicyPackBuilder.Domain.Enums;
using PolicyPackBuilder.Infrastructure.Data;

namespace PolicyPackBuilder.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for WorkflowRun persistence.
/// </summary>
public sealed class WorkflowRunRepository : IWorkflowRunRepository
{
    private readonly PolicyPackDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the WorkflowRunRepository.
    /// </summary>
    public WorkflowRunRepository(PolicyPackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<WorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowRun?> GetByIdWithStepsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _dbContext.WorkflowRuns.FindAsync([id], cancellationToken);
        if (run == null)
        {
            return null;
        }

        // Load steps separately since we don't have a navigation property
        List<WorkflowStepRun> steps = await _dbContext.WorkflowStepRuns
            .Where(s => s.RunId == id)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);

        // Steps are returned via the step repository, not embedded in run
        return run;
    }

    /// <inheritdoc />
    public async Task<List<WorkflowRun>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<WorkflowRun>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<WorkflowRun>> GetChildRunsAsync(Guid parentRunId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns
            .Where(r => r.ParentRunId == parentRunId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<WorkflowRun>> GetLineageAsync(Guid rootRunId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns
            .Where(r => r.RootRunId == rootRunId || r.Id == rootRunId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowRun> CreateAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowRuns.Update(run);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await GetByIdAsync(id, cancellationToken);
        if (run != null)
        {
            _dbContext.WorkflowRuns.Remove(run);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
