// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OnboardFlow.Application.Interfaces;

namespace OnboardFlow.Infrastructure.LlmClients;

/// <summary>
/// LLM client implementation using IChatClient and ChatClientAgent.
/// </summary>
public sealed class ChatClientLlmClient : ILlmClient
{
    private readonly IChatClient _chatClient;

    public ChatClientLlmClient(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> InvokeAsync(string prompt, string? systemMessage = null, CancellationToken cancellationToken = default)
    {
        ChatClientAgent agent = new(_chatClient, instructions: systemMessage ?? "You are a helpful assistant.");
        ChatMessage message = new(ChatRole.User, prompt);
        AgentResponse response = await agent.RunAsync(message, cancellationToken: cancellationToken);
        return response.Text ?? string.Empty;
    }

    public async Task<string> InvokeForJsonAsync(string prompt, string? systemMessage = null, CancellationToken cancellationToken = default)
    {
        string jsonInstructions = (systemMessage ?? "You are a helpful assistant.") +
            "\n\nIMPORTANT: You MUST respond with valid JSON only. Do not include any explanatory text outside the JSON.";

        ChatOptions chatOptions = new()
        {
            ResponseFormat = ChatResponseFormat.Json
        };

        ChatClientAgentOptions options = new()
        {
            Name = "JsonAgent",
            ChatOptions = chatOptions
        };

        ChatClientAgent agent = new(_chatClient, options);

        string fullPrompt = $"System: {jsonInstructions}\n\nUser: {prompt}";
        ChatMessage message = new(ChatRole.User, fullPrompt);
        AgentResponse response = await agent.RunAsync(message, cancellationToken: cancellationToken);
        return response.Text ?? "{}";
    }
}
