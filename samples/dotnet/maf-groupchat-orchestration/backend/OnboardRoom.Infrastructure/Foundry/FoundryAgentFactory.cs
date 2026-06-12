// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;
using Azure;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace OnboardRoom.Infrastructure.Foundry;

public sealed class FoundryAgentFactory(FoundryOptions options)
{
    private readonly TokenCredential _credential = new AzureCliCredential();

    public AIProjectClient CreateProjectClient()
    {
        if (!Uri.TryCreate(options.ProjectEndpoint, UriKind.Absolute, out Uri? projectEndpoint))
        {
            throw new InvalidOperationException("Configure Foundry:ProjectEndpoint or FOUNDRY_PROJECT_ENDPOINT with your Microsoft Foundry project endpoint.");
        }

        return new(projectEndpoint, this._credential);
    }

    public async Task<IReadOnlyList<AIAgent>> CreateAgentsAsync(AIProjectClient projectClient, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.DeploymentName))
        {
            throw new InvalidOperationException("Configure Foundry:DeploymentName or FOUNDRY_MODEL with your model deployment name.");
        }

        IList<AITool> reviewerTools = await this.CreateReviewerToolsAsync(cancellationToken);

        return
        [
            await CreateHostedAgentAsync(projectClient, AgentNames.Intake, Prompts.Intake, [], cancellationToken),
            await CreateHostedAgentAsync(projectClient, AgentNames.Benefits, Prompts.Benefits, reviewerTools, cancellationToken),
            await CreateHostedAgentAsync(projectClient, AgentNames.Access, Prompts.Access, reviewerTools, cancellationToken),
            await CreateHostedAgentAsync(projectClient, AgentNames.Policy, Prompts.Policy, reviewerTools, cancellationToken),
            await CreateHostedAgentAsync(projectClient, AgentNames.Chair, Prompts.Chair, [], cancellationToken),
        ];
    }

    public async Task DeleteAgentsAsync(AIProjectClient projectClient, IEnumerable<AIAgent> agents)
    {
        foreach (AIAgent agent in agents)
        {
            try
            {
                await projectClient.AgentAdministrationClient.DeleteAgentAsync(agent.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        }
    }

    private async Task<IList<AITool>> CreateReviewerToolsAsync(CancellationToken cancellationToken)
    {
        using HttpClient httpClient = new(new BearerTokenHandler(this._credential, "https://ai.azure.com/.default")
        {
            InnerHandler = new HttpClientHandler(),
        });

        await using McpClient mcpClient = await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri(BuildToolboxMcpEndpoint()),
                    Name = "onboardroom_foundry_toolbox",
                    TransportMode = HttpTransportMode.StreamableHttp,
                    AdditionalHeaders = new Dictionary<string, string>
                    {
                        ["Foundry-Features"] = "Toolboxes=V1Preview",
                    },
                },
                httpClient),
            cancellationToken: cancellationToken);

        IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
        return [.. tools.Cast<AITool>()];
    }

    private async Task<FoundryAgent> CreateHostedAgentAsync(
        AIProjectClient projectClient,
        string name,
        string instructions,
        IList<AITool> tools,
        CancellationToken cancellationToken)
    {
        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
            name,
            new ProjectsAgentVersionCreationOptions(
                new DeclarativeAgentDefinition(model: options.DeploymentName)
                {
                    Instructions = instructions,
                }));

        return projectClient.AsAIAgent(agentVersion, tools);
    }

    private string BuildToolboxMcpEndpoint()
        => $"{options.ProjectEndpoint.TrimEnd('/')}/toolboxes/{options.ToolboxName}/mcp?api-version={Uri.EscapeDataString(options.ToolboxApiVersion)}";

    private sealed class BearerTokenHandler(TokenCredential credential, string scope) : DelegatingHandler
    {
        private readonly TokenRequestContext _tokenContext = new([scope]);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AccessToken token = await credential.GetTokenAsync(this._tokenContext, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

internal static class Prompts
{
    public const string Intake =
        """
        You are the intake specialist for OnboardRoom. Normalize the request into facts, unknowns, dependencies, and time-sensitive items.
        Hand the room a concise structured intake brief.
        """;

    public const string Benefits =
        """
        You are the benefits and employee experience guide. Use toolbox tools when current policy, regional benefits, or onboarding guidance needs verification.
        Focus on benefits orientation, required forms, employee moments, and first-week support.
        """;

    public const string Access =
        """
        You are the access engineer. Use toolbox tools when you need to verify Microsoft, Azure, GitHub, or identity-related setup guidance.
        Focus on accounts, devices, VPN, GitHub, least-privilege access, and readiness blockers.
        """;

    public const string Policy =
        """
        You are the policy and compliance reviewer. Use toolbox tools when current regulatory, security, or internal-policy-like guidance needs grounding.
        Focus on data residency, field safety, conditional access, and approval risks.
        """;

    public const string Chair =
        """
        You are the group chat chair. Synthesize the room's findings into a final onboarding plan.
        Include: decision, owner-by-owner action list, blockers, approvals, and a next 48-hour checklist.
        Be crisp and avoid repeating every previous message.
        """;
}
