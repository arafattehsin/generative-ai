// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using OnboardRoom.Application.Utilities;

namespace OnboardRoom.Tests;

public sealed class JsonRepairTests
{
    [Fact]
    public void TryDeserializeFromPossiblyFencedJson_extracts_fenced_json_and_repairs_trailing_commas()
    {
        const string text =
            """
            Chair summary:
            ```json
            { "decision": "Proceed", "riskLevel": "Managed", }
            ```
            """;

        Result? result = JsonRepair.TryDeserializeFromPossiblyFencedJson<Result>(text);

        result.Should().NotBeNull();
        result!.Decision.Should().Be("Proceed");
        result.RiskLevel.Should().Be("Managed");
    }

    private sealed record Result(string Decision, string RiskLevel);
}
