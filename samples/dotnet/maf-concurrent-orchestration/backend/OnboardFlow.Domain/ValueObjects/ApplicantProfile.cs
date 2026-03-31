// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

namespace OnboardFlow.Domain.ValueObjects;

/// <summary>
/// Structured applicant profile extracted from the onboarding request.
/// </summary>
public sealed class ApplicantProfile
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Region { get; set; }
    public List<string> RequestedFeatures { get; set; } = [];
    public List<string> RequestedIntegrations { get; set; } = [];
    public string? BillingPreference { get; set; }
    public List<string> SecurityRequirements { get; set; } = [];
    public List<string> ComplianceRequirements { get; set; } = [];
    public string? AdditionalNotes { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ApplicantProfile FromJson(string json)
        => JsonSerializer.Deserialize<ApplicantProfile>(json, JsonOptions) ?? new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
