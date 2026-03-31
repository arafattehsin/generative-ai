// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Domain.Enums;

/// <summary>
/// Status of an individual workflow step.
/// </summary>
public enum StepStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4
}
