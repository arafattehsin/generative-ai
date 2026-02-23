using System.Collections.Concurrent;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.ChatClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace IncidentCommandCenter.Api.Services;

public sealed class AgentRuntime
{
    private readonly AIAgent _agent;
    private readonly SkillRunContextAccessor _runContextAccessor;
    private readonly ILogger<AgentRuntime> _logger;
    private readonly ConcurrentDictionary<string, AgentThread> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public AgentRuntime(
        IHostEnvironment environment,
        ISkillCatalogService skillCatalog,
        ISkillTraceStore traceStore,
        SkillRunContextAccessor runContextAccessor,
        ILogger<AgentRuntime> logger,
        ILoggerFactory loggerFactory)
    {
        _runContextAccessor = runContextAccessor;
        _logger = logger;

        string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? Environment.GetEnvironmentVariable("AOI_ENDPOINT_SWDN")
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

        string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
            ?? Environment.GetEnvironmentVariable("AzureOpenAI__Deployment")
            ?? "gpt-4o";

        string? apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? Environment.GetEnvironmentVariable("AOI_KEY_SWDN");

        AzureOpenAIClient client = string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        var chatClient = client.GetChatClient(deployment);

        ChatClientAgentOptions options = new()
        {
            Name = "IncidentCommandCenterAgent",
            ChatOptions = new()
            {
                Instructions = "You are a supply chain disruption specialist. Use the native skills tools when domain expertise is needed.",
            },
            AIContextProviderFactory = _ => new NativeAgentSkillsContextProvider(
                skillCatalog,
                traceStore,
                _runContextAccessor,
                loggerFactory.CreateLogger<NativeAgentSkillsContextProvider>()),
        };

        _agent = chatClient.CreateAIAgent(options);

        _logger.LogInformation(
            "Agent runtime initialized. Environment={EnvironmentName}, Deployment={Deployment}, EndpointHost={EndpointHost}",
            environment.EnvironmentName,
            deployment,
            new Uri(endpoint).Host);

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
        _logger.LogInformation("Created agent session {SessionId}", sessionId);
        _ = cancellationToken;
        return Task.FromResult(sessionId);
    }

    public bool SessionExists(string sessionId) => _sessions.ContainsKey(sessionId);

    public async Task<string> RunAsync(
        string sessionId,
        string prompt,
        string? runId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out AgentThread? session))
        {
            throw new KeyNotFoundException($"Session '{sessionId}' does not exist.");
        }

        using IDisposable scope = _runContextAccessor.Push(runId);
        _logger.LogInformation(
            "Starting agent run. SessionId={SessionId}, RunId={RunId}, PromptChars={PromptChars}",
            sessionId,
            runId ?? "<none>",
            prompt.Length);

        ChatMessage userMessage = new(ChatRole.User, [new TextContent(prompt)]);
        var response = await _agent.RunAsync([userMessage], session);
        _logger.LogInformation(
            "Completed agent run. SessionId={SessionId}, RunId={RunId}, ResponseChars={ResponseChars}",
            sessionId,
            runId ?? "<none>",
            response.Text?.Length ?? 0);
        _ = cancellationToken;
        return response.Text ?? string.Empty;
    }
}
