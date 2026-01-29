// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Base interface for workflow step executors.
/// </summary>
public interface IStepExecutor
{
    /// <summary>
    /// The name of this step.
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Executes the step and returns the updated context.
    /// </summary>
    Task<WorkflowContext> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default);
}
