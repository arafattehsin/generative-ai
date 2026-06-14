// Copyright (c) Microsoft. All rights reserved.

using OnboardRoom.Domain.Entities;

namespace OnboardRoom.Application.Interfaces;

public interface IRunRepository
{
    Task AddAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    Task<WorkflowRun?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRun>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRun>> LineageAsync(Guid rootRunId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
