// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.SignalR;
using OnboardFlow.Application.Interfaces;

namespace OnboardFlow.Api.Hubs;

/// <summary>
/// SignalR hub for real-time workflow progress updates.
/// </summary>
public sealed class RunsHub : Hub
{
    public async Task JoinRun(Guid runId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"run-{runId}");

    public async Task LeaveRun(Guid runId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"run-{runId}");
}

/// <summary>
/// Implementation of IWorkflowProgressHub using SignalR.
/// </summary>
public sealed class WorkflowProgressHubService : IWorkflowProgressHub
{
    private readonly IHubContext<RunsHub> _hubContext;

    public WorkflowProgressHubService(IHubContext<RunsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendStepStartedAsync(Guid runId, string stepName, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepStarted",
            new { RunId = runId, StepName = stepName, StartedAt = DateTime.UtcNow },
            cancellationToken);
    }

    public async Task SendStepCompletedAsync(Guid runId, string stepName, int durationMs, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepCompleted",
            new { RunId = runId, StepName = stepName, DurationMs = durationMs, CompletedAt = DateTime.UtcNow },
            cancellationToken);
    }

    public async Task SendStepFailedAsync(Guid runId, string stepName, string error, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepFailed",
            new { RunId = runId, StepName = stepName, Error = error, FailedAt = DateTime.UtcNow },
            cancellationToken);
    }

    public async Task SendRunCompletedAsync(Guid runId, bool success, string? error, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "RunCompleted",
            new { RunId = runId, Success = success, Error = error, CompletedAt = DateTime.UtcNow },
            cancellationToken);
    }

    public async Task SendStepStatusAsync(Guid runId, string stepName, string statusMessage, int? progressPercent = null, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepStatus",
            new { RunId = runId, StepName = stepName, StatusMessage = statusMessage, ProgressPercent = progressPercent },
            cancellationToken);
    }

    public async Task SendConcurrentGroupStartedAsync(Guid runId, string[] reviewerNames, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "ConcurrentGroupStarted",
            new { RunId = runId, ReviewerNames = reviewerNames, StartedAt = DateTime.UtcNow },
            cancellationToken);
    }

    public async Task SendBarrierReleasedAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "BarrierReleased",
            new { RunId = runId, ReleasedAt = DateTime.UtcNow },
            cancellationToken);
    }
}
