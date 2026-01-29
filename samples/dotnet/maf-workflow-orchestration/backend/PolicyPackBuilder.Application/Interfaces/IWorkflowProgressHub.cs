// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Application.Interfaces;

/// <summary>
/// Hub interface for sending real-time workflow updates to connected clients.
/// </summary>
public interface IWorkflowProgressHub
{
    /// <summary>
    /// Notifies clients that a step has started.
    /// </summary>
    Task SendStepStartedAsync(Guid runId, string stepName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients that a step has completed.
    /// </summary>
    Task SendStepCompletedAsync(Guid runId, string stepName, int durationMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients that a step has failed.
    /// </summary>
    Task SendStepFailedAsync(Guid runId, string stepName, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients that the workflow run has completed.
    /// </summary>
    Task SendRunCompletedAsync(Guid runId, bool success, string? error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends detailed status message for current step operation.
    /// </summary>
    Task SendStepStatusAsync(Guid runId, string stepName, string statusMessage, int? progressPercent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams partial output from LLM as it generates.
    /// </summary>
    Task SendLlmStreamAsync(Guid runId, string stepName, string partialText, CancellationToken cancellationToken = default);
}
