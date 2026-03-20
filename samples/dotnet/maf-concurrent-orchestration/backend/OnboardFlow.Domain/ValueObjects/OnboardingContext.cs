// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnboardFlow.Domain.ValueObjects;

/// <summary>
/// Carries data between workflow steps through the pipeline.
/// </summary>
public sealed class OnboardingContext
{
    public string NormalizedInput { get; set; } = string.Empty;

    public string? ApplicantProfileJson { get; set; }

    public string? SecurityReviewJson { get; set; }
    public string? ComplianceReviewJson { get; set; }
    public string? FinanceReviewJson { get; set; }

    public string? DecisionPackJson { get; set; }
    public string? CustomerNextSteps { get; set; }
    public string? FinalHtml { get; set; }

    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Original input text (before PII redaction).
    /// </summary>
    [JsonIgnore]
    public string OriginalInput { get; set; } = string.Empty;

    public List<RedactedItem> RedactedItems { get; set; } = [];

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public static OnboardingContext FromJson(string json)
    {
        return JsonSerializer.Deserialize<OnboardingContext>(json, JsonSerializerOptions)
            ?? new OnboardingContext();
    }

    public OnboardingContext Clone()
    {
        string json = ToJson();
        OnboardingContext cloned = FromJson(json);
        cloned.OriginalInput = OriginalInput;
        return cloned;
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
/// Represents a redacted PII item for audit logging.
/// </summary>
public sealed class RedactedItem
{
    public string Type { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public int Position { get; set; }
}
