// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using OnboardFlow.Application.Interfaces;
using OnboardFlow.Domain.Entities;
using OnboardFlow.Domain.Enums;
using OnboardFlow.Infrastructure.Data;

namespace OnboardFlow.Infrastructure.Repositories;

public sealed class WorkflowRunRepository : IWorkflowRunRepository
{
    private readonly OnboardFlowDbContext _dbContext;

    public WorkflowRunRepository(OnboardFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.WorkflowRuns.FindAsync([id], cancellationToken);

    public async Task<List<WorkflowRun>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowRun>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowRun>> GetLineageAsync(Guid rootRunId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRuns
            .Where(r => r.RootRunId == rootRunId || r.Id == rootRunId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowRun> CreateAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task UpdateAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowRuns.Update(run);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await GetByIdAsync(id, cancellationToken);
        if (run != null)
        {
            _dbContext.WorkflowRuns.Remove(run);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
