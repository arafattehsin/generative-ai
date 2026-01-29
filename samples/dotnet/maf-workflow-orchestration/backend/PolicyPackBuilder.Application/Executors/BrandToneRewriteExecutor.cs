// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Domain.Enums;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Step 5: Rewrite the text for the desired brand tone using LLM.
/// </summary>
public sealed class BrandToneRewriteExecutor : IStepExecutor
{
    private readonly ILlmClient _llmClient;

    /// <summary>
    /// Initializes a new instance of the BrandToneRewriteExecutor.
    /// </summary>
    public BrandToneRewriteExecutor(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    /// <inheritdoc />
    public string StepName => WorkflowSteps.BrandToneRewrite;

    /// <inheritdoc />
    public async Task<WorkflowContext> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default)
    {
        string toneDescription = context.Options.Tone switch
        {
            ToneType.Professional => "Professional: Formal, authoritative, business-appropriate. Use clear, direct language that conveys expertise and reliability.",
            ToneType.Friendly => "Friendly: Warm, approachable, conversational but respectful. Use inclusive language that feels welcoming and supportive.",
            ToneType.Formal => "Formal: Highly structured, official, ceremonial. Use precise language appropriate for legal or official documents.",
            _ => "Professional"
        };

        string systemMessage = $"""
            You are a brand voice specialist. Rewrite the text to match the target tone while preserving all content.
            
            TARGET TONE: {toneDescription}
            
            CRITICAL REQUIREMENTS - YOU MUST:
            1. Preserve ALL factual content and meaning exactly
            2. Keep ALL disclaimers exactly as written - do not modify, summarize, or remove them
            3. Maintain ALL legal/compliance language unchanged
            4. Only adjust style, word choice, and phrasing around the protected content
            
            The rewrite should feel natural while meeting all these requirements.
            """;

        string prompt = $"""
            Rewrite the following text to match the {context.Options.Tone} tone:
            
            --- TEXT TO REWRITE ---
            {context.CompliantText}
            --- END TEXT ---
            
            Remember: Preserve all facts, disclaimers, and compliance language. Only adjust the tone and style.
            
            Output ONLY the rewritten text, no explanations or preamble.
            """;

        string response = await _llmClient.InvokeAsync(prompt, systemMessage, cancellationToken);

        context.ToneRewrittenText = response;

        return context;
    }
}
