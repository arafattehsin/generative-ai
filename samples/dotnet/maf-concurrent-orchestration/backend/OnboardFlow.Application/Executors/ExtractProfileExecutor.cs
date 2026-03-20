// Copyright (c) Microsoft. All rights reserved.

using OnboardFlow.Application.Interfaces;
using OnboardFlow.Domain.ValueObjects;

namespace OnboardFlow.Application.Executors;

/// <summary>
/// Step 2: Extract structured applicant profile from the onboarding request using LLM.
/// </summary>
public sealed class ExtractProfileExecutor : IStepExecutor
{
    private readonly ILlmClient _llmClient;

    public ExtractProfileExecutor(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    public string StepName => WorkflowSteps.ExtractProfile;

    public async Task<OnboardingContext> ExecuteAsync(OnboardingContext context, CancellationToken cancellationToken = default)
    {
        string systemMessage = """
            You are a B2B onboarding analyst. Extract structured information from the onboarding request.
            
            Output STRICT JSON only with this exact schema:
            {
              "companyName": "string",
              "contactName": "string or null",
              "region": "string or null (e.g. EU, US, APAC)",
              "requestedFeatures": ["feature1", "feature2"],
              "requestedIntegrations": ["integration1", "integration2"],
              "billingPreference": "string or null",
              "securityRequirements": ["requirement1"],
              "complianceRequirements": ["requirement1"],
              "additionalNotes": "string or null"
            }
            
            Extract all relevant details. If a field is not mentioned, use null or an empty array.
            Do not include any text outside the JSON object.
            """;

        string prompt = $"""
            Analyze the following onboarding request and extract the applicant profile:
            
            ---
            {context.NormalizedInput}
            ---
            
            Output only valid JSON matching the specified schema.
            """;

        string response = await _llmClient.InvokeForJsonAsync(prompt, systemMessage, cancellationToken);
        context.ApplicantProfileJson = response;

        return context;
    }
}
