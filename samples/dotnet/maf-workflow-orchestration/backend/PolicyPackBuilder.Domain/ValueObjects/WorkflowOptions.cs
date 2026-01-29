// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Domain.ValueObjects;

/// <summary>
/// Configuration options for a workflow run.
/// </summary>
public sealed class WorkflowOptions
{
    /// <summary>
    /// Target audience for the policy output.
    /// </summary>
    public AudienceType Audience { get; set; } = AudienceType.Customer;

    /// <summary>
    /// Desired tone for the output text.
    /// </summary>
    public ToneType Tone { get; set; } = ToneType.Professional;

    /// <summary>
    /// If true, enforces strict compliance rules including required disclaimers.
    /// </summary>
    public bool StrictCompliance { get; set; } = false;

    /// <summary>
    /// Serializes the options to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    /// <summary>
    /// Deserializes options from JSON.
    /// </summary>
    public static WorkflowOptions FromJson(string json)
    {
        return JsonSerializer.Deserialize<WorkflowOptions>(json, JsonSerializerOptions)
            ?? new WorkflowOptions();
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
