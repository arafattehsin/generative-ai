// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolicyPackBuilder.Domain.ValueObjects;

/// <summary>
/// Carries data between workflow steps through the sequential pipeline.
/// </summary>
public sealed class WorkflowContext
{
    /// <summary>
    /// Normalized input text after intake processing.
    /// </summary>
    public string NormalizedInput { get; set; } = string.Empty;

    /// <summary>
    /// JSON output from the Extract Facts step.
    /// </summary>
    public string? ExtractedFactsJson { get; set; }

    /// <summary>
    /// Draft customer summary from the summarization step.
    /// </summary>
    public string? DraftCustomerSummary { get; set; }

    /// <summary>
    /// JSON output from compliance check (issues found).
    /// </summary>
    public string? ComplianceIssuesJson { get; set; }

    /// <summary>
    /// Compliant text after compliance fixes applied.
    /// </summary>
    public string? CompliantText { get; set; }

    /// <summary>
    /// Text after brand tone rewriting.
    /// </summary>
    public string? ToneRewrittenText { get; set; }

    /// <summary>
    /// Final HTML output.
    /// </summary>
    public string? FinalHtml { get; set; }

    /// <summary>
    /// Warnings accumulated during workflow execution.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Workflow configuration options.
    /// </summary>
    public WorkflowOptions Options { get; set; } = new();

    /// <summary>
    /// Original input text (before PII redaction).
    /// </summary>
    [JsonIgnore]
    public string OriginalInput { get; set; } = string.Empty;

    /// <summary>
    /// List of redacted PII items for audit purposes.
    /// </summary>
    public List<RedactedItem> RedactedItems { get; set; } = [];

    /// <summary>
    /// Serializes the context to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    /// <summary>
    /// Deserializes context from JSON.
    /// </summary>
    public static WorkflowContext FromJson(string json)
    {
        return JsonSerializer.Deserialize<WorkflowContext>(json, JsonSerializerOptions)
            ?? new WorkflowContext();
    }

    /// <summary>
    /// Creates a deep copy of this context.
    /// </summary>
    public WorkflowContext Clone()
    {
        string json = ToJson();
        WorkflowContext cloned = FromJson(json);
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
    /// <summary>
    /// Type of PII (e.g., "email", "phone").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Placeholder used in redacted text.
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Position in original text where PII was found.
    /// </summary>
    public int Position { get; set; }
}
