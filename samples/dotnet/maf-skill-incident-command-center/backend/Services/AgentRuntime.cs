using System.Collections.Concurrent;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace IncidentCommandCenter.Api.Services;

public sealed class AgentRuntime
{
    private readonly AIAgent _agent;
    private readonly SkillRunContextAccessor _runContextAccessor;
    private readonly ILogger<AgentRuntime> _logger;
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

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

        IChatClient chatClient = client.GetChatClient(deployment).AsIChatClient();

        ChatClientAgentOptions options = new()
        {
            Name = "IncidentCommandCenterAgent",
            ChatOptions = new()
            {
                Instructions = "You are a supply chain disruption specialist. Use the native skills tools when domain expertise is needed.",
            },
            AIContextProviders =
            [
                new NativeAgentSkillsContextProvider(
                    skillCatalog,
                    traceStore,
                    _runContextAccessor,
                    loggerFactory.CreateLogger<NativeAgentSkillsContextProvider>())
            ],
        };

        _agent = chatClient.AsAIAgent(options);

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

    public async Task<string> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        AgentSession session = await _agent.CreateSessionAsync(cancellationToken);
        string sessionId = $"session-{Guid.NewGuid():N}";
        _sessions[sessionId] = session;
        _logger.LogInformation("Created agent session {SessionId}", sessionId);
        return sessionId;
    }

    public bool SessionExists(string sessionId) => _sessions.ContainsKey(sessionId);

    public async Task<string> RunAsync(
        string sessionId,
        string prompt,
        string? runId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out AgentSession? session))
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
