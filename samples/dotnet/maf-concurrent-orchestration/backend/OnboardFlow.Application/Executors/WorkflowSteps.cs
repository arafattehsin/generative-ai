// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Application.Executors;

/// <summary>
/// Defines the workflow step names in execution order.
/// Steps 3a-3c are the concurrent review stage.
/// </summary>
public static class WorkflowSteps
{
    public const string IntakeNormalize = "IntakeNormalize";
    public const string ExtractProfile = "ExtractProfile";
    public const string SecurityReview = "SecurityReview";
    public const string ComplianceReview = "ComplianceReview";
    public const string FinanceReview = "FinanceReview";
    public const string AggregateFindings = "AggregateFindings";
    public const string CustomerNextSteps = "CustomerNextSteps";
    public const string FinalPackage = "FinalPackage";

    /// <summary>
    /// Concurrent reviewer step names.
    /// </summary>
    public static readonly string[] ConcurrentReviewers =
    [
        SecurityReview,
        ComplianceReview,
        FinanceReview
    ];

    /// <summary>
    /// All steps in logical execution order.
    /// </summary>
    public static readonly string[] AllSteps =
    [
        IntakeNormalize,
        ExtractProfile,
        SecurityReview,
        ComplianceReview,
        FinanceReview,
        AggregateFindings,
        CustomerNextSteps,
        FinalPackage
    ];

    public const string ConcurrencyGroupName = "ConcurrentReview";

    public static int GetStepOrder(string stepName)
    {
        int index = Array.IndexOf(AllSteps, stepName);
        return index >= 0 ? index + 1 : 0;
    }

    public static bool IsConcurrentStep(string stepName)
    {
        return Array.IndexOf(ConcurrentReviewers, stepName) >= 0;
    }

    public static string GetStepDescription(string stepName)
    {
        return stepName switch
        {
            IntakeNormalize => "Normalizes whitespace, cleans up formatting, redacts PII.",
            ExtractProfile => "Extracts structured applicant profile from the onboarding request.",
            SecurityReview => "Reviews security risks, integration controls, SSO/SCIM concerns.",
            ComplianceReview => "Checks data residency, regulatory flags, contract terms.",
            FinanceReview => "Evaluates billing requirements, credit risks, invoice needs.",
            AggregateFindings => "Merges all reviewer findings into a single Decision Pack.",
            CustomerNextSteps => "Generates a concise customer-facing next steps message.",
            FinalPackage => "Formats the output as structured HTML with full audit trail.",
            _ => "Unknown step."
        };
    }
}
