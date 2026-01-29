// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.SignalR;
using PolicyPackBuilder.Application.Interfaces;

namespace PolicyPackBuilder.Api.Hubs;

/// <summary>
/// SignalR hub for real-time workflow progress updates.
/// </summary>
public sealed class RunsHub : Hub
{
    /// <summary>
    /// Client joins a specific run's group to receive updates.
    /// </summary>
    public async Task JoinRun(Guid runId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"run-{runId}");
    }

    /// <summary>
    /// Client leaves a specific run's group.
    /// </summary>
    public async Task LeaveRun(Guid runId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"run-{runId}");
    }
}

/// <summary>
/// Implementation of IWorkflowProgressHub using SignalR.
/// </summary>
public sealed class WorkflowProgressHubService : IWorkflowProgressHub
{
    private readonly IHubContext<RunsHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the WorkflowProgressHubService.
    /// </summary>
    public WorkflowProgressHubService(IHubContext<RunsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task SendStepStartedAsync(Guid runId, string stepName, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepStarted",
            new { RunId = runId, StepName = stepName, StartedAt = DateTime.UtcNow },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendStepCompletedAsync(Guid runId, string stepName, int durationMs, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepCompleted",
            new { RunId = runId, StepName = stepName, DurationMs = durationMs, CompletedAt = DateTime.UtcNow },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendStepFailedAsync(Guid runId, string stepName, string error, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepFailed",
            new { RunId = runId, StepName = stepName, Error = error, FailedAt = DateTime.UtcNow },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendRunCompletedAsync(Guid runId, bool success, string? error, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "RunCompleted",
            new { RunId = runId, Success = success, Error = error, CompletedAt = DateTime.UtcNow },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendStepStatusAsync(Guid runId, string stepName, string statusMessage, int? progressPercent = null, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "StepStatus",
            new { RunId = runId, StepName = stepName, StatusMessage = statusMessage, ProgressPercent = progressPercent, Timestamp = DateTime.UtcNow },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendLlmStreamAsync(Guid runId, string stepName, string partialText, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"run-{runId}").SendAsync(
            "LlmStream",
            new { RunId = runId, StepName = stepName, PartialText = partialText, Timestamp = DateTime.UtcNow },
            cancellationToken);
    }
}
