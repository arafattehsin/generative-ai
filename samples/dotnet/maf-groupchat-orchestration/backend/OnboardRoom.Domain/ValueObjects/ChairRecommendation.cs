// Copyright (c) Microsoft. All rights reserved.

namespace OnboardRoom.Domain.ValueObjects;

public sealed record ChairRecommendation(
    string Decision,
    string RiskLevel,
    int Confidence,
    IReadOnlyList<string> Owners,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> Next48Hours)
{
    public static ChairRecommendation Pending { get; } = new(
        Decision: "Pending",
        RiskLevel: "Unknown",
        Confidence: 0,
        Owners: [],
        Blockers: [],
        Next48Hours: []);
}
