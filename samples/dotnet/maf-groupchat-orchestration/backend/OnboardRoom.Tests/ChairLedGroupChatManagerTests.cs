// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using OnboardRoom.Infrastructure.Foundry;

namespace OnboardRoom.Tests;

public sealed class ChairLedGroupChatManagerTests
{
    [Theory]
    [InlineData(0, AgentNames.Intake)]
    [InlineData(1, AgentNames.Benefits)]
    [InlineData(2, AgentNames.Access)]
    [InlineData(3, AgentNames.Policy)]
    [InlineData(4, AgentNames.Chair)]
    [InlineData(9, AgentNames.Chair)]
    public void SelectSpeakerName_routes_specialists_before_chair(int iteration, string expected)
    {
        ChairLedGroupChatManager.SelectSpeakerName(iteration).Should().Be(expected);
    }
}
