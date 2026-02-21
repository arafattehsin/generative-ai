using System.Collections.Concurrent;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.ChatClient;
using Microsoft.Extensions.AI;
using OpenAI;

namespace IncidentCommandCenter.Api.Services;

public sealed class AgentRuntime
{
    private readonly AIAgent _agent;
    private readonly ConcurrentDictionary<string, AgentThread> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public AgentRuntime(IHostEnvironment environment)
    {
        string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

        string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
            ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

        string? apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        AzureOpenAIClient client = string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        var chatClient = client.GetChatClient(deployment);

        _agent = chatClient.CreateAIAgent(
            instructions: "You are a supply chain disruption specialist. Use provided skill references and templates in your reasoning.",
            tools: []);

        _ = environment;
    }

    public void ValidateConfiguration()
    {
        // Construction already validates required runtime config.
    }

    public Task<string> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        AgentThread session = _agent.GetNewThread();
        string sessionId = $"session-{Guid.NewGuid():N}";
        _sessions[sessionId] = session;
        _ = cancellationToken;
        return Task.FromResult(sessionId);
    }

    public bool SessionExists(string sessionId) => _sessions.ContainsKey(sessionId);

    public async Task<string> RunAsync(string sessionId, string prompt, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out AgentThread? session))
        {
            throw new KeyNotFoundException($"Session '{sessionId}' does not exist.");
        }

        ChatMessage userMessage = new(ChatRole.User, [new TextContent(prompt)]);
        var response = await _agent.RunAsync([userMessage], session);
        _ = cancellationToken;
        return response.Text ?? string.Empty;
    }
}
