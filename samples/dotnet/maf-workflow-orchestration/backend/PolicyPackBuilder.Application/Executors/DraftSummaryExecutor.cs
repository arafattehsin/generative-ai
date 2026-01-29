// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Domain.Enums;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Step 3: Draft a customer-friendly summary using LLM.
/// </summary>
public sealed class DraftSummaryExecutor : IStepExecutor
{
    private readonly ILlmClient _llmClient;

    /// <summary>
    /// Initializes a new instance of the DraftSummaryExecutor.
    /// </summary>
    public DraftSummaryExecutor(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    /// <inheritdoc />
    public string StepName => WorkflowSteps.DraftSummary;

    /// <inheritdoc />
    public async Task<WorkflowContext> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default)
    {
        string audienceDescription = context.Options.Audience switch
        {
            AudienceType.Customer => "external customers who may not be familiar with technical or legal jargon",
            AudienceType.Internal => "internal employees who understand company context",
            AudienceType.Legal => "legal professionals who expect precise, formal language",
            _ => "a general audience"
        };

        string systemMessage = $"""
            You are a professional communications writer. Create a concise, clear summary of this policy/communication.
            
            Requirements:
            - Write for {audienceDescription}
            - Use plain English, avoid jargon
            - Be concise (max 300 words)
            - Highlight key takeaways
            - Maintain accuracy to source material
            - Structure with clear sections if appropriate
            - Do not invent information not present in the source
            """;

        string prompt = $"""
            Create a customer-friendly summary of the following policy document:
            
            --- ORIGINAL DOCUMENT ---
            {context.NormalizedInput}
            --- END DOCUMENT ---
            
            --- EXTRACTED FACTS ---
            {context.ExtractedFactsJson}
            --- END FACTS ---
            
            Write a clear, concise summary suitable for the target audience.
            """;

        string response = await _llmClient.InvokeAsync(prompt, systemMessage, cancellationToken);

        context.DraftCustomerSummary = response;

        return context;
    }
}
