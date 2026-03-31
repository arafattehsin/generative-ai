// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Application.Interfaces;

/// <summary>
/// Abstraction for LLM invocation.
/// </summary>
public interface ILlmClient
{
    Task<string> InvokeAsync(
        string prompt,
        string? systemMessage = null,
        CancellationToken cancellationToken = default);

    Task<string> InvokeForJsonAsync(
        string prompt,
        string? systemMessage = null,
        CancellationToken cancellationToken = default);
}
