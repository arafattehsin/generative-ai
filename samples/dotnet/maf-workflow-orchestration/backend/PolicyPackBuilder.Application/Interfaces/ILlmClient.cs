// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Application.Interfaces;

/// <summary>
/// Abstraction for LLM (Large Language Model) invocation.
/// This interface allows swapping between different LLM providers.
/// </summary>
/// <remarks>
/// Current implementation uses ChatClientAgent with AzureOpenAIClient.
/// TODO: Upgrade to PersistentAgentsClient for multi-session workflows
/// when implementing long-running agent scenarios.
/// </remarks>
public interface ILlmClient
{
    /// <summary>
    /// Invokes the LLM with a prompt and optional system message.
    /// </summary>
    /// <param name="prompt">The user prompt to send to the LLM.</param>
    /// <param name="systemMessage">Optional system message for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LLM response text.</returns>
    Task<string> InvokeAsync(
        string prompt,
        string? systemMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the LLM expecting a JSON response.
    /// </summary>
    /// <param name="prompt">The user prompt to send to the LLM.</param>
    /// <param name="systemMessage">Optional system message for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LLM response as parsed JSON string.</returns>
    Task<string> InvokeForJsonAsync(
        string prompt,
        string? systemMessage = null,
        CancellationToken cancellationToken = default);
}
