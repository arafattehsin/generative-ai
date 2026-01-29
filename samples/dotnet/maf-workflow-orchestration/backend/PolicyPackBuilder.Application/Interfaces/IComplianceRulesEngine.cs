// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Application.Interfaces;

/// <summary>
/// Engine for checking and enforcing compliance rules.
/// </summary>
public interface IComplianceRulesEngine
{
    /// <summary>
    /// Checks the text for compliance issues.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="audience">Target audience type.</param>
    /// <param name="strictMode">Whether to enforce strict compliance.</param>
    /// <returns>Compliance check result.</returns>
    ComplianceResult CheckCompliance(string text, AudienceType audience, bool strictMode);

    /// <summary>
    /// Gets the required disclaimers for the specified audience and mode.
    /// </summary>
    List<string> GetRequiredDisclaimers(AudienceType audience, bool strictMode);

    /// <summary>
    /// Gets the list of restricted phrases.
    /// </summary>
    List<string> GetRestrictedPhrases();
}

/// <summary>
/// Result of a compliance check.
/// </summary>
public sealed class ComplianceResult
{
    /// <summary>
    /// Whether the text is compliant.
    /// </summary>
    public bool IsCompliant { get; init; }

    /// <summary>
    /// List of compliance issues found.
    /// </summary>
    public List<ComplianceIssue> Issues { get; init; } = [];

    /// <summary>
    /// Required disclaimers that should be present.
    /// </summary>
    public List<string> RequiredDisclaimers { get; init; } = [];

    /// <summary>
    /// Disclaimers that are missing from the text.
    /// </summary>
    public List<string> MissingDisclaimers { get; init; } = [];
}

/// <summary>
/// A single compliance issue.
/// </summary>
public sealed class ComplianceIssue
{
    /// <summary>
    /// Type of issue (e.g., "restricted_phrase", "missing_disclaimer").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description of the issue.
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// Severity level (low, med, high).
    /// </summary>
    public string Severity { get; init; } = "low";

    /// <summary>
    /// The problematic text fragment, if applicable.
    /// </summary>
    public string? Fragment { get; init; }
}
