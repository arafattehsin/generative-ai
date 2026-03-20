// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Application.Interfaces;

/// <summary>
/// Hub interface for sending real-time workflow updates to connected clients.
/// </summary>
public interface IWorkflowProgressHub
{
    Task SendStepStartedAsync(Guid runId, string stepName, CancellationToken cancellationToken = default);
    Task SendStepCompletedAsync(Guid runId, string stepName, int durationMs, CancellationToken cancellationToken = default);
    Task SendStepFailedAsync(Guid runId, string stepName, string error, CancellationToken cancellationToken = default);
    Task SendRunCompletedAsync(Guid runId, bool success, string? error, CancellationToken cancellationToken = default);
    Task SendStepStatusAsync(Guid runId, string stepName, string statusMessage, int? progressPercent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients that the concurrent review group has started.
    /// </summary>
    Task SendConcurrentGroupStartedAsync(Guid runId, string[] reviewerNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients that all concurrent reviewers have completed and the barrier has released.
    /// </summary>
    Task SendBarrierReleasedAsync(Guid runId, CancellationToken cancellationToken = default);
}
