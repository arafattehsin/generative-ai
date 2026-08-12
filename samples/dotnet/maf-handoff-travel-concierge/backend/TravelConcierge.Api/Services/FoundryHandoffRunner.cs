using Azure.AI.Projects;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;
using TravelConcierge.Api.Models;

namespace TravelConcierge.Api.Services;

public sealed class FoundryHandoffRunner(
    FoundryOptions options,
    FoundryCredentialProvider credentialProvider,
    HumanSupportWorkflowService humanSupport) : ITravelConciergeRunner, IDisposable
{
    private readonly SemaphoreSlim _workflowLock = new(1, 1);
    private IReadOnlyDictionary<string, Workflow>? _workflows;

    public async Task ProcessTurnAsync(TravelRun run, string userMessage, RunEventWriter writer, CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
        {
            throw new InvalidOperationException($"{FoundryOptions.EndpointEnvironmentVariable} is not configured.");
        }

        await writer.SetStatusAsync(Models.RunStatus.Running, "Live Agent Framework handoff is processing the traveller turn.", cancellationToken);

        Workflow workflow = await GetWorkflowAsync(run.CurrentOwnerId, cancellationToken).ConfigureAwait(false);
        List<ChatMessage> prompt = [new(ChatRole.User, BuildTurnPrompt(run, userMessage))];
        await using StreamingRun session = await InProcessExecution.RunStreamingAsync(workflow, prompt, cancellationToken: cancellationToken).ConfigureAwait(false);
        await session.TrySendMessageAsync(new TurnToken(emitEvents: true)).ConfigureAwait(false);

        string? speakingAgentName = null;
        AgentDefinition? speakingAgent = null;
        StringBuilder responseBuffer = new();
        bool hasAgentResponse = false;
        bool humanEscalationStarted = false;
        AgentDefinition? humanRequestedBy = null;

        async Task FlushAgentResponseAsync()
        {
            if (speakingAgent is null || responseBuffer.Length == 0)
            {
                return;
            }

            if (speakingAgent.Id == TravelAgents.Human.Id)
            {
                await humanSupport.StartAsync(
                    run,
                    humanRequestedBy ?? TravelAgents.Triage,
                    responseBuffer.ToString(),
                    writer,
                    cancellationToken).ConfigureAwait(false);
                humanEscalationStarted = true;
            }
            else
            {
                await writer.AddAgentMessageAsync(speakingAgent, responseBuffer.ToString(), cancellationToken)
                    .ConfigureAwait(false);
            }

            responseBuffer.Clear();
            hasAgentResponse = true;
        }

        await foreach (WorkflowEvent evt in session.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent update:
                {
                    // Whitespace-only stream chunks carry word and list boundaries.
                    if (string.IsNullOrEmpty(update.Update.Text))
                    {
                        break;
                    }

                    string authorName = update.Update.AuthorName ?? TravelAgents.Triage.Name;
                    AgentDefinition agent = TravelAgents.Resolve(authorName);

                    if (!string.Equals(speakingAgentName, authorName, StringComparison.Ordinal))
                    {
                        await FlushAgentResponseAsync().ConfigureAwait(false);

                        AgentDefinition from = TravelAgents.Resolve(run.CurrentOwnerId);
                        if (!string.Equals(from.Id, agent.Id, StringComparison.Ordinal))
                        {
                            if (agent.Id == TravelAgents.Human.Id)
                            {
                                humanRequestedBy = from;
                            }

                            await writer.HandoffAsync(from, agent, $"Live handoff selected {agent.Name} as current owner.", cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.SetOwnerAsync(agent, cancellationToken).ConfigureAwait(false);
                        }

                        speakingAgentName = authorName;
                        speakingAgent = agent;
                    }

                    responseBuffer.Append(update.Update.Text);
                    break;
                }

                case WorkflowOutputEvent output when output.Data is IEnumerable<ChatMessage> outputMessages && !hasAgentResponse && responseBuffer.Length == 0:
                    bool wroteOutputMessage = false;
                    foreach (ChatMessage message in outputMessages.Where(message =>
                                 message.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(message.Text)))
                    {
                        string authorName = message.AuthorName ?? TravelAgents.Resolve(run.CurrentOwnerId).Name;
                        AgentDefinition agent = TravelAgents.Resolve(authorName);
                        AgentDefinition from = TravelAgents.Resolve(run.CurrentOwnerId);

                        if (!string.Equals(from.Id, agent.Id, StringComparison.Ordinal))
                        {
                            if (agent.Id == TravelAgents.Human.Id)
                            {
                                humanRequestedBy = from;
                            }

                            await writer.HandoffAsync(from, agent, $"Live handoff selected {agent.Name} as current owner.", cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.SetOwnerAsync(agent, cancellationToken).ConfigureAwait(false);
                        }

                        if (agent.Id == TravelAgents.Human.Id)
                        {
                            await humanSupport.StartAsync(
                                run,
                                humanRequestedBy ?? from,
                                message.Text,
                                writer,
                                cancellationToken).ConfigureAwait(false);
                            humanEscalationStarted = true;
                        }
                        else
                        {
                            await writer.AddAgentMessageAsync(agent, message.Text, cancellationToken).ConfigureAwait(false);
                        }

                        hasAgentResponse = true;
                        wroteOutputMessage = true;
                    }

                    if (!wroteOutputMessage)
                    {
                        await writer.AddSystemMessageAsync("We couldn't complete that response. Please try again.", cancellationToken)
                            .ConfigureAwait(false);
                        await writer.SetStatusAsync(Models.RunStatus.Failed, "The specialist returned no response.", cancellationToken)
                            .ConfigureAwait(false);
                        return;
                    }
                    break;

                case WorkflowOutputEvent output when output.Data is not null && !hasAgentResponse && responseBuffer.Length == 0:
                    await writer.AddSystemMessageAsync(output.Data.ToString() ?? string.Empty, cancellationToken).ConfigureAwait(false);
                    break;

                case WorkflowWarningEvent warning when warning.Data is string message:
                    await writer.AddSystemMessageAsync(message, cancellationToken).ConfigureAwait(false);
                    break;

                case WorkflowErrorEvent error:
                    string errorText = error.Exception?.Message ?? "Unknown workflow error.";
                    await writer.AddSystemMessageAsync($"Workflow error: {errorText}", cancellationToken).ConfigureAwait(false);
                    await writer.SetStatusAsync(Models.RunStatus.Failed, "Live handoff failed.", cancellationToken).ConfigureAwait(false);
                    return;

                case ExecutorFailedEvent failed:
                    await writer.AddSystemMessageAsync(FormatExecutorFailure(failed), cancellationToken).ConfigureAwait(false);
                    await writer.SetStatusAsync(Models.RunStatus.Failed, "Live handoff failed.", cancellationToken).ConfigureAwait(false);
                    return;
            }
        }

        await FlushAgentResponseAsync().ConfigureAwait(false);
        if (humanEscalationStarted)
        {
            return;
        }

        await writer.SetStatusAsync(Models.RunStatus.WaitingForUser, "Waiting for the traveller's next message.", cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _workflowLock.Dispose();
    }

    private async Task<Workflow> GetWorkflowAsync(string ownerId, CancellationToken cancellationToken)
    {
        if (_workflows != null)
        {
            return ResolveWorkflow(_workflows, ownerId);
        }

        await _workflowLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_workflows != null)
            {
                return ResolveWorkflow(_workflows, ownerId);
            }

            AIProjectClient projectClient = new(
                new Uri(options.ProjectEndpoint),
                await credentialProvider.GetCredentialAsync(cancellationToken).ConfigureAwait(false));
            IChatClient chatClient = projectClient.ProjectOpenAIClient
                .GetChatClient(options.DeploymentName)
                .AsIChatClient();

            TravelAgentRegistry agents = new(chatClient);
            Dictionary<string, Microsoft.Agents.AI.AIAgent> workflowRoots = new(StringComparer.OrdinalIgnoreCase)
            {
                [TravelAgents.Triage.Id] = agents.TriageAgent,
                [TravelAgents.Flight.Id] = agents.FlightAgent,
                [TravelAgents.Stay.Id] = agents.StayAgent,
                [TravelAgents.Insurance.Id] = agents.InsuranceAgent,
                [TravelAgents.Loyalty.Id] = agents.LoyaltyAgent,
                [TravelAgents.Human.Id] = agents.HumanAgent,
            };
            Microsoft.Agents.AI.AIAgent[] allAgents = workflowRoots.Values.ToArray();
            Dictionary<string, Workflow> workflows = new(StringComparer.OrdinalIgnoreCase);

            foreach ((string id, Microsoft.Agents.AI.AIAgent rootAgent) in workflowRoots)
            {
                HandoffWorkflowBuilder builder = AgentWorkflowBuilder.CreateHandoffBuilderWith(rootAgent);
                foreach (Microsoft.Agents.AI.AIAgent destination in allAgents)
                {
                    builder.WithHandoffs(allAgents.Except([destination]), destination);
                }

                workflows[id] = builder.Build();
            }

            _workflows = workflows;
            return ResolveWorkflow(_workflows, ownerId);
        }
        finally
        {
            _workflowLock.Release();
        }
    }

    private static Workflow ResolveWorkflow(IReadOnlyDictionary<string, Workflow> workflows, string ownerId) =>
        workflows.TryGetValue(ownerId, out Workflow? workflow)
            ? workflow
            : workflows[TravelAgents.Triage.Id];

    private static string BuildTurnPrompt(TravelRun run, string userMessage)
    {
        IEnumerable<string> recentMessages = run.Messages
            .TakeLast(6)
            .Select(message => $"{message.AuthorName}: {CompactConversationText(message.Text, message.AuthorType == ConversationAuthor.User ? 700 : 360)}");

        return $"""
            Traveller: {run.TravellerName}
            Trip code: {run.TripCode}
            Urgency: {run.Urgency}
            Current owner before this turn: {run.CurrentOwnerName}
            Latest traveller message: {userMessage}

            Recent conversation:
            {string.Join(Environment.NewLine, recentMessages)}

            Continue the travel disruption recovery conversation. If the latest request belongs to another specialist, hand off ownership before answering and let the receiving specialist answer it.

            Conversation response contract:
            - The purpose of this demo is to make ownership and the traveller's immediate decision obvious.
            - Answer only the latest need. Use the context already provided; do not repeat the case, ask for a known fact, or require the traveller to restate anything carried through a handoff.
            - Stay under 80 words.
            - If one missing detail blocks the decision, ask only that one question and do not create a card or preliminary checklist.
            - Otherwise, begin with one plain-language sentence that states the decision or outcome.
            - If the latest message answers the previous question, accept that answer directly and move the recovery forward. Do not paraphrase the full case back to the traveller.
            - Use at most one bold heading, followed by no more than three short items. Use a heading specific to the decision, such as **Flight decision**, **Tonight's stay**, **Save for your claim**, or **Recovery plan**.
            - Never use generic headings such as Next steps, What happens next, Key details, What to ask, At the desk, Before you accept, Keep, or Submit first.
            - End with at most one short question, and only when its answer changes the next decision. Put the question after the card as the final sentence, never inside the list.
            - Do not provide scripts, exhaustive checklists, speculative routes, broad policy explanations, or loosely related guidance unless explicitly requested.
            - Use plain Markdown only. Do not emit JSON or Adaptive Card schema.
            """;
    }

    private static string CompactConversationText(string text, int maxLength)
    {
        string compact = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= maxLength ? compact : $"{compact[..maxLength]}...";
    }

    private static string FormatExecutorFailure(ExecutorFailedEvent failed)
    {
        string details = failed.Data?.ToString() ?? string.Empty;

        if (details.Contains("CredentialUnavailableException", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("AuthenticationFailedException", StringComparison.OrdinalIgnoreCase))
        {
            return "Foundry sign-in could not be completed. Retry the chat and complete the Microsoft sign-in window if it appears.";
        }

        string firstLine = details.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? "Unknown executor failure.";
        return $"Executor failed: {failed.ExecutorId}. {firstLine}";
    }
}
