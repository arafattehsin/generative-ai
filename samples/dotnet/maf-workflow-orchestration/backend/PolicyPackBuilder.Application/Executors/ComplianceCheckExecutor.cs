// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Step 4: Check and fix compliance issues using LLM and rules engine.
/// </summary>
public sealed class ComplianceCheckExecutor : IStepExecutor
{
    private readonly ILlmClient _llmClient;
    private readonly IComplianceRulesEngine _rulesEngine;

    /// <summary>
    /// Initializes a new instance of the ComplianceCheckExecutor.
    /// </summary>
    public ComplianceCheckExecutor(ILlmClient llmClient, IComplianceRulesEngine rulesEngine)
    {
        _llmClient = llmClient;
        _rulesEngine = rulesEngine;
    }

    /// <inheritdoc />
    public string StepName => WorkflowSteps.ComplianceCheck;

    /// <inheritdoc />
    public async Task<WorkflowContext> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default)
    {
        // First, run non-LLM compliance check
        ComplianceResult rulesResult = _rulesEngine.CheckCompliance(
            context.DraftCustomerSummary ?? context.NormalizedInput,
            context.Options.Audience,
            context.Options.StrictCompliance);

        // Add warnings for rule violations
        foreach (ComplianceIssue issue in rulesResult.Issues)
        {
            context.Warnings.Add($"Compliance: {issue.Detail} (Severity: {issue.Severity})");
        }

        string strictModeInstruction = context.Options.StrictCompliance
            ? "STRICT MODE ENABLED: You MUST ensure ALL of the following disclaimers are included in the fixed_text, word for word: " +
              string.Join("; ", rulesResult.RequiredDisclaimers)
            : "Include appropriate disclaimers based on the content type.";

        string restrictedPhrasesJson = JsonSerializer.Serialize(_rulesEngine.GetRestrictedPhrases());

        string jsonExample = """{"issues": [{"type": "restricted_phrase|missing_disclaimer|ambiguous|legal_risk", "detail": "Description", "severity": "low|med|high"}], "fixed_text": "The corrected text with all issues addressed"}""";

        string systemMessage = $"""
            You are a compliance reviewer. Analyze the text for compliance issues and fix them.
            
            Check for:
            - Restricted phrases that could be misleading: {restrictedPhrasesJson}
            - Missing required disclaimers
            - Unclear or ambiguous language
            - Potential legal risks
            - Overpromising or absolute guarantees
            
            Output STRICT JSON only. Example format:
            {jsonExample}
            
            {strictModeInstruction}
            
            IMPORTANT: The fixed_text should be the complete corrected version, not just a description of changes.
            """;

        string prompt = $"""
            Review and fix the following text for compliance:
            
            --- TEXT TO REVIEW ---
            {context.DraftCustomerSummary}
            --- END TEXT ---
            
            Required disclaimers for this audience:
            {string.Join("\n", rulesResult.RequiredDisclaimers.Select(d => $"- {d}"))}
            
            Currently missing disclaimers:
            {string.Join("\n", rulesResult.MissingDisclaimers.Select(d => $"- {d}"))}
            
            Output the compliance analysis and fixed text as JSON.
            """;

        string response = await _llmClient.InvokeForJsonAsync(prompt, systemMessage, cancellationToken);

        context.ComplianceIssuesJson = response;

        // Parse the response to extract fixed text
        try
        {
            using JsonDocument doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("fixed_text", out JsonElement fixedTextElement))
            {
                context.CompliantText = fixedTextElement.GetString() ?? context.DraftCustomerSummary;
            }
            else
            {
                context.CompliantText = context.DraftCustomerSummary;
                context.Warnings.Add("Could not extract fixed_text from compliance response. Using original summary.");
            }
        }
        catch (JsonException)
        {
            context.CompliantText = context.DraftCustomerSummary;
            context.Warnings.Add("Failed to parse compliance response JSON. Using original summary.");
        }

        return context;
    }
}
