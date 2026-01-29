// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Domain.Enums;

/// <summary>
/// Represents the overall status of a workflow run.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// The workflow run has been created but not yet started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The workflow run is currently executing.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The workflow run completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The workflow run failed due to an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The workflow run was canceled by the user.
    /// </summary>
    Canceled = 4
}
