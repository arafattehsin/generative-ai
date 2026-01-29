// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Application.Services;

/// <summary>
/// Engine for checking and enforcing compliance rules.
/// </summary>
public sealed class ComplianceRulesEngine : IComplianceRulesEngine
{
    private static readonly List<string> RestrictedPhrases =
    [
        "guaranteed",
        "100% safe",
        "no risk",
        "risk-free",
        "absolutely certain",
        "promise",
        "never fail",
        "always works",
        "unconditional",
        "unlimited liability"
    ];

    private static readonly Dictionary<AudienceType, List<string>> RequiredDisclaimersByAudience = new()
    {
        [AudienceType.Customer] =
        [
            "Terms and conditions apply.",
            "Please review the full policy document for complete details."
        ],
        [AudienceType.Internal] =
        [
            "For internal use only. Do not distribute externally."
        ],
        [AudienceType.Legal] =
        [
            "This document does not constitute legal advice.",
            "Consult with legal counsel before taking action based on this document."
        ]
    };

    private static readonly List<string> StrictModeAdditionalDisclaimers =
    [
        "All information is subject to change without notice.",
        "Past performance does not guarantee future results."
    ];

    /// <summary>
    /// Checks the text for compliance issues.
    /// </summary>
    public ComplianceResult CheckCompliance(string text, AudienceType audience, bool strictMode)
    {
        List<ComplianceIssue> issues = [];
        string lowerText = text.ToLowerInvariant();

        // Check for restricted phrases
        foreach (string phrase in RestrictedPhrases)
        {
            if (lowerText.Contains(phrase.ToLowerInvariant()))
            {
                issues.Add(new ComplianceIssue
                {
                    Type = "restricted_phrase",
                    Detail = $"The phrase '{phrase}' is restricted and should be avoided.",
                    Severity = "med",
                    Fragment = phrase
                });
            }
        }

        // Get required disclaimers
        List<string> requiredDisclaimers = GetRequiredDisclaimers(audience, strictMode);
        List<string> missingDisclaimers = [];

        // Check for missing disclaimers
        foreach (string disclaimer in requiredDisclaimers)
        {
            if (!text.Contains(disclaimer, StringComparison.OrdinalIgnoreCase))
            {
                missingDisclaimers.Add(disclaimer);
                issues.Add(new ComplianceIssue
                {
                    Type = "missing_disclaimer",
                    Detail = $"Required disclaimer missing: '{disclaimer}'",
                    Severity = strictMode ? "high" : "med",
                    Fragment = disclaimer
                });
            }
        }

        return new ComplianceResult
        {
            IsCompliant = issues.Count == 0,
            Issues = issues,
            RequiredDisclaimers = requiredDisclaimers,
            MissingDisclaimers = missingDisclaimers
        };
    }

    /// <summary>
    /// Gets the required disclaimers for the specified audience and mode.
    /// </summary>
    public List<string> GetRequiredDisclaimers(AudienceType audience, bool strictMode)
    {
        List<string> disclaimers = [];

        if (RequiredDisclaimersByAudience.TryGetValue(audience, out List<string>? audienceDisclaimers))
        {
            disclaimers.AddRange(audienceDisclaimers);
        }

        if (strictMode)
        {
            disclaimers.AddRange(StrictModeAdditionalDisclaimers);
        }

        return disclaimers;
    }

    /// <summary>
    /// Gets the list of restricted phrases.
    /// </summary>
    public List<string> GetRestrictedPhrases()
    {
        return [.. RestrictedPhrases];
    }
}
