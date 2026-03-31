// Copyright (c) Microsoft. All rights reserved.

using OnboardFlow.Application.Interfaces;
using OnboardFlow.Domain.ValueObjects;

namespace OnboardFlow.Application.Executors;

/// <summary>
/// Step 5: Generate a concise customer-facing next steps message (≤200 words).
/// </summary>
public sealed class CustomerNextStepsExecutor : IStepExecutor
{
    private readonly ILlmClient _llmClient;

    public CustomerNextStepsExecutor(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    public string StepName => WorkflowSteps.CustomerNextSteps;

    public async Task<OnboardingContext> ExecuteAsync(OnboardingContext context, CancellationToken cancellationToken = default)
    {
        string systemMessage = """
            You are a customer success manager. Write a concise, professional message
            summarizing the onboarding decision and next steps.
            
            Requirements:
            - Maximum 200 words
            - Professional but friendly tone
            - Clear action items for the customer
            - Reference the overall recommendation
            - Do not include internal findings or confidential details
            """;

        string prompt = $"""
            Based on the following Decision Pack, write a customer-facing next steps message:
            
            --- DECISION PACK ---
            {context.DecisionPackJson}
            --- END ---
            
            --- APPLICANT PROFILE ---
            {context.ApplicantProfileJson}
            --- END ---
            
            Write a concise message (≤200 words) for the customer.
            """;

        string response = await _llmClient.InvokeAsync(prompt, systemMessage, cancellationToken);
        context.CustomerNextSteps = response;

        return context;
    }
}
