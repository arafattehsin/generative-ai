// Copyright (c) Microsoft. All rights reserved.

using OnboardFlow.Domain.Entities;
using OnboardFlow.Domain.Enums;

namespace OnboardFlow.Application.Interfaces;

public interface IWorkflowRunRepository
{
    Task<WorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<WorkflowRun>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<List<WorkflowRun>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
    Task<List<WorkflowRun>> GetLineageAsync(Guid rootRunId, CancellationToken cancellationToken = default);
    Task<WorkflowRun> CreateAsync(WorkflowRun run, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowRun run, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWorkflowStepRunRepository
{
    Task<WorkflowStepRun?> GetByRunIdAndStepNameAsync(Guid runId, string stepName, CancellationToken cancellationToken = default);
    Task<List<WorkflowStepRun>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowStepRun>> GetCompletedStepsBeforeAsync(Guid runId, int stepOrder, CancellationToken cancellationToken = default);
    Task<WorkflowStepRun> CreateAsync(WorkflowStepRun step, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowStepRun step, CancellationToken cancellationToken = default);
}
