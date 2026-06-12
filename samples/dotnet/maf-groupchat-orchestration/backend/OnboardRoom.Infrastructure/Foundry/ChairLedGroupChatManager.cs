// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace OnboardRoom.Infrastructure.Foundry;

public sealed class ChairLedGroupChatManager(IReadOnlyList<AIAgent> agents) : GroupChatManager
{
    private readonly AIAgent _intake = agents.Single(agent => agent.Name == AgentNames.Intake);
    private readonly AIAgent _benefits = agents.Single(agent => agent.Name == AgentNames.Benefits);
    private readonly AIAgent _access = agents.Single(agent => agent.Name == AgentNames.Access);
    private readonly AIAgent _policy = agents.Single(agent => agent.Name == AgentNames.Policy);
    private readonly AIAgent _chair = agents.Single(agent => agent.Name == AgentNames.Chair);

    public static string SelectSpeakerName(int iterationCount) => iterationCount switch
    {
        0 => AgentNames.Intake,
        1 => AgentNames.Benefits,
        2 => AgentNames.Access,
        3 => AgentNames.Policy,
        _ => AgentNames.Chair,
    };

    protected override ValueTask<AIAgent> SelectNextAgentAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
        => new(SelectSpeakerName(this.IterationCount) switch
        {
            AgentNames.Intake => this._intake,
            AgentNames.Benefits => this._benefits,
            AgentNames.Access => this._access,
            AgentNames.Policy => this._policy,
            _ => this._chair,
        });

    protected override ValueTask<bool> ShouldTerminateAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
        => new(this.IterationCount >= 5 || this.MaximumIterationCount <= this.IterationCount);
}

public static class AgentNames
{
    public const string Intake = "onboardroom-intake";
    public const string Benefits = "onboardroom-benefits";
    public const string Access = "onboardroom-access";
    public const string Policy = "onboardroom-policy";
    public const string Chair = "onboardroom-chair";
}
