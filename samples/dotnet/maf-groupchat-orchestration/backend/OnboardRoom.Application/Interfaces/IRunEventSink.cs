// Copyright (c) Microsoft. All rights reserved.

using OnboardRoom.Application.Dtos;
using OnboardRoom.Domain.Entities;

namespace OnboardRoom.Application.Interfaces;

public interface IRunEventSink
{
    Task StepStartedAsync(Guid runId, WorkflowStepRun step, CancellationToken cancellationToken = default);

    Task StepCompletedAsync(Guid runId, WorkflowStepRun step, CancellationToken cancellationToken = default);

    Task StepFailedAsync(Guid runId, WorkflowStepRun step, CancellationToken cancellationToken = default);

    Task GroupChatMessageReceivedAsync(Guid runId, GroupChatMessage message, CancellationToken cancellationToken = default);

    Task GroupChatCompletedAsync(Guid runId, bool maxRoundsReached, CancellationToken cancellationToken = default);

    Task RunCompletedAsync(RunDetailDto run, CancellationToken cancellationToken = default);
}
