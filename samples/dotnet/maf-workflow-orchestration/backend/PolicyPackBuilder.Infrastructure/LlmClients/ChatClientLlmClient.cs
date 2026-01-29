// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using PolicyPackBuilder.Application.Interfaces;

namespace PolicyPackBuilder.Infrastructure.LlmClients;

/// <summary>
/// LLM client implementation using IChatClient and ChatClientAgent pattern from workflow samples.
/// </summary>
public sealed class ChatClientLlmClient : ILlmClient
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the ChatClientLlmClient.
    /// </summary>
    public ChatClientLlmClient(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <inheritdoc />
    public async Task<string> InvokeAsync(string prompt, string? systemMessage = null, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[LLM] Starting regular completion using ChatClientAgent...");

        try
        {
            // Create a ChatClientAgent with system message as instructions
            ChatClientAgent agent = new(_chatClient, instructions: systemMessage ?? "You are a helpful assistant.");

            // Run the agent with the user prompt
            ChatMessage message = new(ChatRole.User, prompt);
            AgentResponse response = await agent.RunAsync(message, cancellationToken: cancellationToken);

            string responseText = response.Text ?? string.Empty;
            Console.WriteLine($"[LLM] Completion successful - Response length: {responseText.Length}");

            return responseText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LLM ERROR] {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> InvokeForJsonAsync(string prompt, string? systemMessage = null, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[LLM] Starting JSON completion using ChatClientAgent...");

        try
        {
            // Create a ChatClientAgent with JSON output instructions
            string jsonInstructions = (systemMessage ?? "You are a helpful assistant.") +
                "\n\nIMPORTANT: You MUST respond with valid JSON only. Do not include any explanatory text outside the JSON.";

            // Create agent with JSON response format
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

            // Run the agent with the user prompt - include system message in the prompt since options doesn't have Instructions
            string fullPrompt = $"System: {jsonInstructions}\n\nUser: {prompt}";
            ChatMessage message = new(ChatRole.User, fullPrompt);
            AgentResponse response = await agent.RunAsync(message, cancellationToken: cancellationToken);

            string responseText = response.Text ?? "{}";
            Console.WriteLine($"[LLM] JSON completion successful - Response length: {responseText.Length}");

            return responseText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LLM ERROR] {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
