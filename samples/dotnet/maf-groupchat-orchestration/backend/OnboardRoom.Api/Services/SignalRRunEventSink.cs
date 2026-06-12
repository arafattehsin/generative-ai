// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.SignalR;
using OnboardRoom.Api.Hubs;
using OnboardRoom.Application.Dtos;
using OnboardRoom.Application.Interfaces;
using OnboardRoom.Domain.Entities;

namespace OnboardRoom.Api.Services;

public sealed class SignalRRunEventSink(IHubContext<RunsHub> hubContext) : IRunEventSink
{
    public async Task StepStartedAsync(Guid runId, WorkflowStepRun step, CancellationToken cancellationToken = default)
        => await SendAsync(runId, "stepStarted", step.StepName, null, step.OutputSnapshot, cancellationToken);

    public async Task StepCompletedAsync(Guid runId, WorkflowStepRun step, CancellationToken cancellationToken = default)
        => await SendAsync(runId, "stepCompleted", step.StepName, null, step.OutputSnapshot, cancellationToken);

    public async Task StepFailedAsync(Guid runId, WorkflowStepRun step, CancellationToken cancellationToken = default)
        => await SendAsync(runId, "stepFailed", step.StepName, null, step.Error, cancellationToken);

    public async Task GroupChatMessageReceivedAsync(Guid runId, GroupChatMessage message, CancellationToken cancellationToken = default)
        => await SendAsync(runId, "groupChatMessageReceived", null, message.Speaker, message.Content, cancellationToken);

    public async Task GroupChatCompletedAsync(Guid runId, bool maxRoundsReached, CancellationToken cancellationToken = default)
        => await SendAsync(runId, "groupChatCompleted", null, null, maxRoundsReached ? "Maximum rounds reached" : "Chair completed the room", cancellationToken);

    public async Task RunCompletedAsync(RunDetailDto run, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(run.Id.ToString()).SendAsync("runCompleted", run, cancellationToken);
        await hubContext.Clients.Group("runs").SendAsync("runCompleted", run, cancellationToken);
    }

    private async Task SendAsync(
        Guid runId,
        string kind,
        string? stepName,
        string? speaker,
        string? content,
        CancellationToken cancellationToken)
    {
        WorkflowEventDto dto = new(runId, kind, stepName, speaker, content, DateTime.UtcNow);
        await hubContext.Clients.Group(runId.ToString()).SendAsync(kind, dto, cancellationToken);
        await hubContext.Clients.Group("runs").SendAsync(kind, dto, cancellationToken);
    }
}
