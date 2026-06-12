// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OnboardRoom.Application.Interfaces;
using OnboardRoom.Domain.Entities;

namespace OnboardRoom.Infrastructure.Foundry;

public sealed class FoundryGroupChatWorkflowRunner(FoundryAgentFactory agentFactory) : IGroupChatWorkflowRunner
{
    public async Task<GroupChatResult> RunAsync(
        WorkflowRun run,
        string roomBrief,
        IRunEventSink eventSink,
        CancellationToken cancellationToken = default)
    {
        AIProjectClient projectClient = agentFactory.CreateProjectClient();
        IReadOnlyList<AIAgent> agents = await agentFactory.CreateAgentsAsync(projectClient, cancellationToken);
        List<GroupChatMessage> messages = [];
        GroupChatMessage? currentMessage = null;
        int sequence = 0;

        try
        {
            ChairLedGroupChatManager manager = new(agents)
            {
                MaximumIterationCount = run.MaxRounds,
            };

            Workflow workflow = AgentWorkflowBuilder
                .CreateGroupChatBuilderWith(_ => manager)
                .AddParticipants(agents)
                .WithName("OnboardRoom API group chat")
                .WithDescription("Chair-led multi-agent onboarding boardroom.")
                .Build();

            List<ChatMessage> prompt = [new(ChatRole.User, roomBrief)];
            await using StreamingRun streamingRun = await InProcessExecution.RunStreamingAsync(workflow, prompt);
            await streamingRun.TrySendMessageAsync(new TurnToken(emitEvents: true));

            await foreach (WorkflowEvent evt in streamingRun.WatchStreamAsync().WithCancellation(cancellationToken))
            {
                if (evt is AgentResponseUpdateEvent update)
                {
                    if (string.IsNullOrWhiteSpace(update.Update.Text))
                    {
                        continue;
                    }

                    if (currentMessage is null || !string.Equals(currentMessage.Speaker, update.ExecutorId, StringComparison.Ordinal))
                    {
                        if (currentMessage is not null)
                        {
                            await eventSink.GroupChatMessageReceivedAsync(run.Id, currentMessage, cancellationToken);
                        }

                        currentMessage = new GroupChatMessage
                        {
                            RunId = run.Id,
                            Speaker = update.ExecutorId,
                            Role = SpeakerRole(update.ExecutorId),
                            Sequence = ++sequence,
                        };
                        messages.Add(currentMessage);
                    }

                    currentMessage.Content += update.Update.Text;
                }

                if (evt is WorkflowErrorEvent error)
                {
                    throw error.Exception ?? new InvalidOperationException("The group chat workflow failed.");
                }
            }

            if (currentMessage is not null)
            {
                await eventSink.GroupChatMessageReceivedAsync(run.Id, currentMessage, cancellationToken);
            }

            bool maxRoundsReached = manager.IterationCount >= run.MaxRounds;
            await eventSink.GroupChatCompletedAsync(run.Id, maxRoundsReached, cancellationToken);

            string chairResponse = string.Join(
                Environment.NewLine,
                messages.Where(message => message.Speaker == AgentNames.Chair).Select(message => message.Content));

            return new GroupChatResult(messages, chairResponse, maxRoundsReached);
        }
        catch (AuthenticationFailedException ex) when (ex.Message.Contains("Please run 'az login'", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("signed out", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Azure CLI authentication is not usable for the Foundry project. Refresh it with: az login --scope https://ai.azure.com/.default && az account set --subscription <subscription-id>",
                ex);
        }
        finally
        {
            await agentFactory.DeleteAgentsAsync(projectClient, agents);
        }
    }

    private static string SpeakerRole(string speaker) => speaker switch
    {
        AgentNames.Intake => "Intake",
        AgentNames.Benefits => "Benefits",
        AgentNames.Access => "Access",
        AgentNames.Policy => "Policy",
        AgentNames.Chair => "Chair",
        _ => "Agent",
    };
}
