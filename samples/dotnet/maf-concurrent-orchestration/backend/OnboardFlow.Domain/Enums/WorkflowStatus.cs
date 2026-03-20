// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Domain.Enums;

/// <summary>
/// Overall status of a workflow run.
/// </summary>
public enum WorkflowStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Canceled = 4
}
