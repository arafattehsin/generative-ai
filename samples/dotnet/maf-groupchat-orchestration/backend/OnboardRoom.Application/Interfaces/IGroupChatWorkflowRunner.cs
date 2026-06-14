// Copyright (c) Microsoft. All rights reserved.

using OnboardRoom.Domain.Entities;

namespace OnboardRoom.Application.Interfaces;

public interface IGroupChatWorkflowRunner
{
    Task<GroupChatResult> RunAsync(
        WorkflowRun run,
        string roomBrief,
        IRunEventSink eventSink,
        CancellationToken cancellationToken = default);
}

public sealed record GroupChatResult(
    IReadOnlyList<GroupChatMessage> Messages,
    string ChairResponse,
    bool MaxRoundsReached);
