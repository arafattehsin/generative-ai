// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Domain.Enums;

/// <summary>
/// Represents the status of an individual workflow step.
/// </summary>
public enum StepStatus
{
    /// <summary>
    /// The step has not yet started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The step is currently executing.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The step completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The step failed due to an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The step was skipped (e.g., during re-run from a later step).
    /// </summary>
    Skipped = 4
}
