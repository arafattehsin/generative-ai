// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using OnboardFlow.Application.Executors;
using OnboardFlow.Application.Interfaces;
using OnboardFlow.Domain.Entities;
using OnboardFlow.Domain.Enums;
using OnboardFlow.Infrastructure.Data;

namespace OnboardFlow.Infrastructure.Repositories;

public sealed class WorkflowStepRunRepository : IWorkflowStepRunRepository
{
    private readonly OnboardFlowDbContext _dbContext;

    public WorkflowStepRunRepository(OnboardFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkflowStepRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.WorkflowStepRuns.FindAsync([id], cancellationToken);

    public async Task<List<WorkflowStepRun>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns
            .Where(s => s.RunId == runId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowStepRun?> GetByRunIdAndStepNameAsync(Guid runId, string stepName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns
            .FirstOrDefaultAsync(s => s.RunId == runId && s.StepName == stepName, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowStepRun>> GetCompletedStepsBeforeAsync(
        Guid runId, int stepOrder, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowStepRuns
            .Where(s => s.RunId == runId && s.StepOrder < stepOrder && s.Status == StepStatus.Completed)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowStepRun> CreateAsync(WorkflowStepRun stepRun, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowStepRuns.Add(stepRun);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return stepRun;
    }

    public async Task UpdateAsync(WorkflowStepRun stepRun, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowStepRuns.Update(stepRun);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
