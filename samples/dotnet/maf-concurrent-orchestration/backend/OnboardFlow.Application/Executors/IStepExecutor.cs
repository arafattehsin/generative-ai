// Copyright (c) Microsoft. All rights reserved.

using OnboardFlow.Domain.ValueObjects;

namespace OnboardFlow.Application.Executors;

/// <summary>
/// Base interface for sequential workflow step executors.
/// </summary>
public interface IStepExecutor
{
    string StepName { get; }
    Task<OnboardingContext> ExecuteAsync(OnboardingContext context, CancellationToken cancellationToken = default);
}
