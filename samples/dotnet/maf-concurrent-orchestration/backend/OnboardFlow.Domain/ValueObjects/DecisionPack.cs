// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

namespace OnboardFlow.Domain.ValueObjects;

/// <summary>
/// Aggregated decision pack produced by the fan-in aggregator.
/// </summary>
public sealed class DecisionPack
{
    public string OverallRecommendation { get; set; } = string.Empty;
    public List<string> Conditions { get; set; } = [];
    public List<MergedFinding> MergedFindings { get; set; } = [];
    public List<ReviewConflict> Conflicts { get; set; } = [];
    public List<string> RequiredNextActions { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static DecisionPack FromJson(string json)
        => JsonSerializer.Deserialize<DecisionPack>(json, JsonOptions) ?? new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}

public sealed class MergedFinding
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = [];
}

public sealed class ReviewConflict
{
    public string Topic { get; set; } = string.Empty;
    public List<ConflictOpinion> Opinions { get; set; } = [];
}

public sealed class ConflictOpinion
{
    public string Agent { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
