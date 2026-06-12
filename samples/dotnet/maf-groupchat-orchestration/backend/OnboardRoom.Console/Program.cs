// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using Azure;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Experimental Responses API tool definitions.
#pragma warning disable AAIP001 // Foundry toolboxes are currently preview.

SampleOptions options = SampleOptions.Parse(args);

if (!Uri.TryCreate(options.ProjectEndpoint, UriKind.Absolute, out Uri? projectEndpoint))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Foundry project endpoint is required.");
    Console.ResetColor();
    Console.Error.WriteLine("Set FOUNDRY_PROJECT_ENDPOINT or pass --endpoint <project-endpoint>.");
    return;
}

if (string.IsNullOrWhiteSpace(options.DeploymentName))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Foundry model deployment name is required.");
    Console.ResetColor();
    Console.Error.WriteLine("Set FOUNDRY_MODEL or pass --deployment <deployment-name>.");
    return;
}

TokenCredential credential = new DefaultAzureCredential();
AIProjectClient projectClient = new(projectEndpoint, credential);

Console.WriteLine("OnboardRoom Group Chat Console");
Console.WriteLine($"Foundry project: {projectEndpoint}");
Console.WriteLine($"Deployment: {options.DeploymentName}");
Console.WriteLine($"Manager: {options.Manager}");
Console.WriteLine($"Toolbox: {options.ToolboxName}");
Console.WriteLine();

List<AIAgent> agents = [];
try
{
    string toolboxMcpEndpoint = options.CreateToolbox
        ? await CreateSampleToolboxAsync(options.ToolboxName, projectEndpoint, credential)
        : BuildToolboxMcpEndpoint(projectEndpoint, options.ToolboxName, options.ToolboxApiVersion);

    IList<McpClientTool> mcpTools = await ConnectAndListToolboxToolsAsync(toolboxMcpEndpoint, credential);
    IList<AITool> reviewerTools = [.. mcpTools.Cast<AITool>()];

    Console.WriteLine();
    Console.WriteLine($"Discovered toolbox tools: {string.Join(", ", mcpTools.Select(tool => tool.Name))}");
    Console.WriteLine();

    agents.Add(await CreateHostedAgentAsync(projectClient, AgentNames.Intake, options.DeploymentName, Prompts.Intake));
    agents.Add(await CreateHostedAgentAsync(projectClient, AgentNames.Benefits, options.DeploymentName, Prompts.Benefits, reviewerTools));
    agents.Add(await CreateHostedAgentAsync(projectClient, AgentNames.Access, options.DeploymentName, Prompts.Access, reviewerTools));
    agents.Add(await CreateHostedAgentAsync(projectClient, AgentNames.Policy, options.DeploymentName, Prompts.Policy, reviewerTools));
    agents.Add(await CreateHostedAgentAsync(projectClient, AgentNames.Chair, options.DeploymentName, Prompts.Chair));

    GroupChatManager manager = options.Manager.Equals("roundrobin", StringComparison.OrdinalIgnoreCase)
        ? new RoundRobinGroupChatManager(agents) { MaximumIterationCount = options.MaxRounds }
        : new ChairLedGroupChatManager(agents) { MaximumIterationCount = options.MaxRounds };

    Workflow workflow = AgentWorkflowBuilder
        .CreateGroupChatBuilderWith(_ => manager)
        .AddParticipants(agents)
        .WithName("OnboardRoom group chat")
        .WithDescription("Multi-agent onboarding review room with a chair-led group chat manager.")
        .Build();

    List<ChatMessage> messages =
    [
        new(ChatRole.User, options.Request)
    ];

    Console.WriteLine("Starting group chat...");
    Console.WriteLine(new string('-', 72));

    await RunGroupChatAsync(workflow, messages);
}
catch (Exception ex) when (TryWriteKnownFoundryAccessError(ex, options) || TryWriteKnownAzureAuthenticationError(ex))
{
    Environment.ExitCode = 1;
}
finally
{
    foreach (AIAgent agent in agents)
    {
        try
        {
            await projectClient.AgentAdministrationClient.DeleteAgentAsync(agent.Name);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // The agent was already removed.
        }
    }
}

static bool TryWriteKnownFoundryAccessError(Exception exception, SampleOptions options)
{
    string message = exception.ToString();
    if (!message.Contains("Microsoft.MachineLearningServices/workspaces/agents/action", StringComparison.OrdinalIgnoreCase) &&
        !message.Contains("403", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Foundry access check failed.");
    Console.ResetColor();
    Console.Error.WriteLine("The authenticated Azure identity can reach the project, but it is not authorized to run Foundry agent/toolbox operations.");
    Console.Error.WriteLine("Required action reported by Foundry: Microsoft.MachineLearningServices/workspaces/agents/action");
    Console.Error.WriteLine($"Project endpoint: {options.ProjectEndpoint}");
    Console.Error.WriteLine("Ask a project or subscription administrator to grant this identity an Azure role that includes Foundry agent operations on the project/workspace scope, then rerun the sample.");
    return true;
}

static bool TryWriteKnownAzureAuthenticationError(Exception exception)
{
    string message = exception.ToString();
    if (!message.Contains("AzureCliCredential authentication failed", StringComparison.OrdinalIgnoreCase) &&
        !message.Contains("Please run 'az login'", StringComparison.OrdinalIgnoreCase) &&
        !message.Contains("Account has previously been signed out", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Azure CLI authentication check failed.");
    Console.ResetColor();
    Console.Error.WriteLine("The selected Azure subscription may be correct, but the cached Azure CLI login token is not usable.");
    Console.Error.WriteLine("Run this once, then rerun the sample:");
    Console.Error.WriteLine("az login --scope https://ai.azure.com/.default");
    Console.Error.WriteLine("az account set --subscription <subscription-id>");
    return true;
}

static async Task<FoundryAgent> CreateHostedAgentAsync(
    AIProjectClient projectClient,
    string name,
    string deploymentName,
    string instructions,
    IList<AITool>? tools = null)
{
    ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
        name,
        new ProjectsAgentVersionCreationOptions(
            new DeclarativeAgentDefinition(model: deploymentName)
            {
                Instructions = instructions,
            }));

    return projectClient.AsAIAgent(agentVersion, tools);
}

static async Task RunGroupChatAsync(Workflow workflow, List<ChatMessage> messages)
{
    string? lastExecutorId = null;

    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case AgentResponseUpdateEvent agentUpdate:
                if (!string.Equals(agentUpdate.ExecutorId, lastExecutorId, StringComparison.Ordinal))
                {
                    lastExecutorId = agentUpdate.ExecutorId;
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(agentUpdate.ExecutorId);
                    Console.ResetColor();
                }

                Console.Write(agentUpdate.Update.Text);
                break;

            case WorkflowOutputEvent output:
                Console.WriteLine();
                Console.WriteLine(new string('-', 72));
                if (output.Data is IReadOnlyList<ChatMessage> outputMessages)
                {
                    Console.WriteLine($"Workflow completed with {outputMessages.Count} message(s).");
                }
                else if (output.Data is not null)
                {
                    Console.WriteLine("Workflow output:");
                    Console.WriteLine(output.Data);
                }

                break;

            case WorkflowErrorEvent workflowError:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error occurred.");
                Console.ResetColor();
                break;

            case ExecutorFailedEvent executorFailed:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Executor '{executorFailed.ExecutorId}' failed with {(executorFailed.Data is null ? "unknown error" : $"exception {executorFailed.Data}")}.");
                Console.ResetColor();
                break;
        }
    }
}

static async Task<IList<McpClientTool>> ConnectAndListToolboxToolsAsync(string toolboxMcpEndpoint, TokenCredential credential)
{
    using HttpClient httpClient = new(new BearerTokenHandler(credential, "https://ai.azure.com/.default")
    {
        InnerHandler = new HttpClientHandler(),
    });

    Console.WriteLine($"Connecting to toolbox MCP endpoint: {toolboxMcpEndpoint}");

    await using McpClient mcpClient = await McpClient.CreateAsync(
        new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(toolboxMcpEndpoint),
                Name = "onboardroom_foundry_toolbox",
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Foundry-Features"] = "Toolboxes=V1Preview",
                },
            },
            httpClient));

    return await mcpClient.ListToolsAsync();
}

static async Task<string> CreateSampleToolboxAsync(string name, Uri projectEndpoint, TokenCredential credential)
{
    AgentAdministrationClientOptions options = new();
    options.AddPolicy(new FoundryFeaturesPolicy("Toolboxes=V1Preview"), PipelinePosition.PerCall);
    AgentAdministrationClient adminClient = new(projectEndpoint, credential, options);
    AgentToolboxes toolboxClient = adminClient.GetAgentToolboxes();

    ProjectsAgentTool webSearchTool = WithToolMetadata(
        ProjectsAgentTool.AsProjectTool(ResponseTool.CreateWebSearchTool()),
        name: "onboardroom_web_search",
        description: "Searches the web for current onboarding, benefits, access, and policy information.");
    ProjectsAgentTool microsoftLearnMcpTool = ProjectsAgentTool.AsProjectTool(ResponseTool.CreateMcpTool(
        serverLabel: "microsoft_learn",
        serverUri: new Uri("https://learn.microsoft.com/api/mcp"),
        toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval)));
    ProjectsAgentTool codeInterpreterTool = WithToolMetadata(
        ProjectsAgentTool.AsProjectTool(ResponseTool.CreateCodeInterpreterTool(
            new CodeInterpreterToolContainer(
                CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration([])))),
        name: "onboardroom_code_interpreter",
        description: "Runs small calculations or tabular checks needed during onboarding review.");

    ToolboxVersion created = (await toolboxClient.CreateToolboxVersionAsync(
        name: name,
        tools: [webSearchTool, microsoftLearnMcpTool, codeInterpreterTool],
        description: "OnboardRoom sample toolbox with web search, Microsoft Learn MCP, and code interpreter tools.")).Value;

    Console.WriteLine($"Created toolbox '{created.Name}' v{created.Version} ({created.Tools.Count} tool(s)).");
    return BuildToolboxMcpEndpoint(projectEndpoint, created.Name, Defaults.ToolboxApiVersion);
}

static ProjectsAgentTool WithToolMetadata(ProjectsAgentTool tool, string name, string description)
{
    Type toolType = tool.GetType();
    toolType.GetProperty("Name")?.SetValue(tool, name);
    toolType.GetProperty("Description")?.SetValue(tool, description);
    return tool;
}

static string BuildToolboxMcpEndpoint(Uri projectEndpoint, string toolboxName, string apiVersion)
    => $"{projectEndpoint.ToString().TrimEnd('/')}/toolboxes/{toolboxName}/mcp?api-version={Uri.EscapeDataString(apiVersion)}";

internal sealed class ChairLedGroupChatManager(IReadOnlyList<AIAgent> agents) : GroupChatManager
{
    private readonly AIAgent _intake = agents.Single(agent => agent.Name == AgentNames.Intake);
    private readonly AIAgent _benefits = agents.Single(agent => agent.Name == AgentNames.Benefits);
    private readonly AIAgent _access = agents.Single(agent => agent.Name == AgentNames.Access);
    private readonly AIAgent _policy = agents.Single(agent => agent.Name == AgentNames.Policy);
    private readonly AIAgent _chair = agents.Single(agent => agent.Name == AgentNames.Chair);

    protected override ValueTask<AIAgent> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        AIAgent next = this.IterationCount switch
        {
            0 => this._intake,
            1 => this._benefits,
            2 => this._access,
            3 => this._policy,
            _ => this._chair,
        };

        return new ValueTask<AIAgent>(next);
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        bool chairHasSpoken = this.IterationCount >= 5;
        return new ValueTask<bool>(chairHasSpoken || this.MaximumIterationCount <= this.IterationCount);
    }
}

internal sealed class BearerTokenHandler(TokenCredential credential, string scope) : DelegatingHandler
{
    private readonly TokenRequestContext _tokenContext = new([scope]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AccessToken token = await credential.GetTokenAsync(this._tokenContext, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}

internal sealed class FoundryFeaturesPolicy(string feature) : PipelinePolicy
{
    private const string FeatureHeader = "Foundry-Features";

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        message.Request.Headers.Add(FeatureHeader, feature);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        message.Request.Headers.Add(FeatureHeader, feature);
        return ProcessNextAsync(message, pipeline, currentIndex);
    }
}

internal sealed record SampleOptions(
    string ProjectEndpoint,
    string DeploymentName,
    string ToolboxName,
    string ToolboxApiVersion,
    string Manager,
    int MaxRounds,
    string Request,
    bool CreateToolbox)
{
    public static SampleOptions Parse(string[] args)
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> switches = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = arg[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[++i];
            }
            else
            {
                switches.Add(key);
            }
        }

        return new SampleOptions(
            ProjectEndpoint: GetValue(values, "endpoint", "FOUNDRY_PROJECT_ENDPOINT", "AZURE_AI_PROJECT_ENDPOINT", Defaults.ProjectEndpoint),
            DeploymentName: GetValue(values, "deployment", "FOUNDRY_MODEL", "AZURE_AI_MODEL_DEPLOYMENT_NAME", Defaults.DeploymentName),
            ToolboxName: GetValue(values, "toolbox", "FOUNDRY_TOOLBOX_NAME", null, Defaults.ToolboxName),
            ToolboxApiVersion: GetValue(values, "toolbox-version", "FOUNDRY_TOOLBOX_API_VERSION", "FOUNDRY_AGENT_TOOLSET_API_VERSION", Defaults.ToolboxApiVersion),
            Manager: GetValue(values, "manager", "ONBOARDROOM_MANAGER", null, "chair"),
            MaxRounds: int.TryParse(GetValue(values, "max-rounds", "ONBOARDROOM_MAX_ROUNDS", null, "5"), out int maxRounds) ? maxRounds : 5,
            Request: GetValue(values, "request", "ONBOARDROOM_REQUEST", null, Prompts.DefaultRequest),
            CreateToolbox: switches.Contains("create-toolbox") || bool.TryParse(Environment.GetEnvironmentVariable("FOUNDRY_CREATE_TOOLBOX"), out bool createToolbox) && createToolbox);
    }

    private static string GetValue(
        IReadOnlyDictionary<string, string?> args,
        string argName,
        string envName,
        string? alternateEnvName,
        string fallback)
    {
        if (args.TryGetValue(argName, out string? argValue) && !string.IsNullOrWhiteSpace(argValue))
        {
            return argValue;
        }

        string? envValue = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        if (!string.IsNullOrWhiteSpace(alternateEnvName))
        {
            string? alternateEnvValue = Environment.GetEnvironmentVariable(alternateEnvName);
            if (!string.IsNullOrWhiteSpace(alternateEnvValue))
            {
                return alternateEnvValue;
            }
        }

        return fallback;
    }
}

internal static class Defaults
{
    public const string ProjectEndpoint = "";
    public const string DeploymentName = "";
    public const string ToolboxName = "onboardroom-toolbox";
    public const string ToolboxApiVersion = "v1";
}

internal static class AgentNames
{
    public const string Intake = "onboardroom-intake";
    public const string Benefits = "onboardroom-benefits";
    public const string Access = "onboardroom-access";
    public const string Policy = "onboardroom-policy";
    public const string Chair = "onboardroom-chair";
}

internal static class Prompts
{
    public const string DefaultRequest =
        """
        New employee onboarding request:
        - Persona: senior field engineer joining Contoso Energy
        - Location: Sydney, Australia
        - Start date: next Monday
        - Needs: laptop, VPN, GitHub access, field safety training, benefits orientation, and customer site access
        Coordinate the onboarding plan, identify risks, and produce the chair's final action list.
        """;

    public const string Intake =
        """
        You are the intake specialist for OnboardRoom. Normalize the request into facts, unknowns, dependencies, and time-sensitive items.
        Do not make final decisions. Hand the room a concise, structured intake brief.
        """;

    public const string Benefits =
        """
        You are the benefits and employee experience guide. Use available toolbox tools when current policy, regional benefits, or onboarding guidance needs verification.
        Focus on benefits orientation, required forms, employee moments, and first-week support.
        """;

    public const string Access =
        """
        You are the access engineer. Use available toolbox tools when you need to verify Microsoft, Azure, GitHub, or identity-related setup guidance.
        Focus on accounts, devices, VPN, GitHub, least-privilege access, and readiness blockers.
        """;

    public const string Policy =
        """
        You are the policy and compliance reviewer. Use available toolbox tools when current regulatory, security, or internal-policy-like guidance needs grounding.
        Focus on data residency, field safety, conditional access, and approval risks.
        """;

    public const string Chair =
        """
        You are the group chat chair. Synthesize the room's findings into a final onboarding plan.
        Include: decision, owner-by-owner action list, blockers, approvals, and a next 48-hour checklist.
        Be crisp and avoid repeating every previous message.
        """;
}
