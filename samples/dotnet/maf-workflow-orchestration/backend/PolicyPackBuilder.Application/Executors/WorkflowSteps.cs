// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Defines the workflow step names in execution order.
/// </summary>
public static class WorkflowSteps
{
    /// <summary>
    /// Step 1: Intake and normalize the input text.
    /// </summary>
    public const string IntakeNormalize = "IntakeNormalize";

    /// <summary>
    /// Step 2: Extract facts using LLM.
    /// </summary>
    public const string ExtractFacts = "ExtractFacts";

    /// <summary>
    /// Step 3: Draft customer summary using LLM.
    /// </summary>
    public const string DraftSummary = "DraftSummary";

    /// <summary>
    /// Step 4: Check and fix compliance issues.
    /// </summary>
    public const string ComplianceCheck = "ComplianceCheck";

    /// <summary>
    /// Step 5: Rewrite for brand tone using LLM.
    /// </summary>
    public const string BrandToneRewrite = "BrandToneRewrite";

    /// <summary>
    /// Step 6: Generate final HTML package.
    /// </summary>
    public const string FinalPackage = "FinalPackage";

    /// <summary>
    /// All steps in execution order.
    /// </summary>
    public static readonly string[] AllSteps =
    [
        IntakeNormalize,
        ExtractFacts,
        DraftSummary,
        ComplianceCheck,
        BrandToneRewrite,
        FinalPackage
    ];

    /// <summary>
    /// Gets the step order (1-based) for a step name.
    /// </summary>
    public static int GetStepOrder(string stepName)
    {
        int index = Array.IndexOf(AllSteps, stepName);
        return index >= 0 ? index + 1 : 0;
    }

    /// <summary>
    /// Gets step description for UI display.
    /// </summary>
    public static string GetStepDescription(string stepName)
    {
        return stepName switch
        {
            IntakeNormalize => "Normalizes whitespace, cleans up formatting, and prepares input for processing.",
            ExtractFacts => "Extracts entities, key points, risks, and required disclaimers from the input.",
            DraftSummary => "Generates a concise, customer-friendly summary of the policy.",
            ComplianceCheck => "Checks for restricted phrases, injects required disclaimers, and fixes compliance issues.",
            BrandToneRewrite => "Rewrites the text to match the desired brand tone while preserving meaning.",
            FinalPackage => "Formats the output as structured HTML with metadata and styling.",
            _ => "Unknown step."
        };
    }

    /// <summary>
    /// Gets whether a step uses LLM.
    /// </summary>
    public static bool UsesLlm(string stepName)
    {
        return stepName switch
        {
            ExtractFacts => true,
            DraftSummary => true,
            ComplianceCheck => true,
            BrandToneRewrite => true,
            _ => false
        };
    }
}
