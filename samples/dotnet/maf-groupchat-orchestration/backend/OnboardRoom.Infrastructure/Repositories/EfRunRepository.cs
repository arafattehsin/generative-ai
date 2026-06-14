// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using OnboardRoom.Application.Interfaces;
using OnboardRoom.Domain.Entities;
using OnboardRoom.Infrastructure.Data;

namespace OnboardRoom.Infrastructure.Repositories;

public sealed class EfRunRepository(OnboardRoomDbContext dbContext) : IRunRepository
{
    public async Task AddAsync(WorkflowRun run, CancellationToken cancellationToken = default)
        => await dbContext.WorkflowRuns.AddAsync(run, cancellationToken);

    public async Task<WorkflowRun?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.WorkflowRuns
            .Include(run => run.Steps)
            .Include(run => run.Messages)
            .FirstOrDefaultAsync(run => run.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowRun>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
        => await dbContext.WorkflowRuns
            .Include(run => run.Steps)
            .Include(run => run.Messages)
            .OrderByDescending(run => run.CreatedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowRun>> LineageAsync(Guid rootRunId, CancellationToken cancellationToken = default)
        => await dbContext.WorkflowRuns
            .Include(run => run.Steps)
            .Include(run => run.Messages)
            .Where(run => run.Id == rootRunId || run.RootRunId == rootRunId)
            .OrderBy(run => run.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}
