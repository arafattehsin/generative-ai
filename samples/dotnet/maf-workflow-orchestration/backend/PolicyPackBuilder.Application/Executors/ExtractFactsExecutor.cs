// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Step 2: Extract facts from the input using LLM.
/// Outputs structured JSON with entities, key points, risks, and disclaimers.
/// </summary>
public sealed class ExtractFactsExecutor : IStepExecutor
{
    private readonly ILlmClient _llmClient;

    /// <summary>
    /// Initializes a new instance of the ExtractFactsExecutor.
    /// </summary>
    public ExtractFactsExecutor(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    /// <inheritdoc />
    public string StepName => WorkflowSteps.ExtractFacts;

    /// <inheritdoc />
    public async Task<WorkflowContext> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default)
    {
        string systemMessage = """
            You are a document analysis expert. Extract structured information from the policy/communication text.
            
            Output STRICT JSON only with this exact schema:
            {
              "entities": [{"type": "person|organization|date|amount|product|contact", "value": "..."}],
              "key_points": ["Main point 1", "Main point 2"],
              "risks": ["Potential risk or unclear area"],
              "required_disclaimers": ["Any legally required disclaimers based on content type"]
            }
            
            Guidelines:
            - Extract all contact information (emails shown as placeholders, phone numbers shown as placeholders)
            - Identify key policy points that customers need to understand
            - Flag any risks or unclear language that could cause confusion
            - Suggest disclaimers that should be included based on the content type
            
            Do not include any text outside the JSON object.
            """;

        string prompt = $"""
            Analyze the following policy/communication text and extract structured information:
            
            ---
            {context.NormalizedInput}
            ---
            
            Remember: Output only valid JSON matching the specified schema.
            """;

        string response = await _llmClient.InvokeForJsonAsync(prompt, systemMessage, cancellationToken);

        context.ExtractedFactsJson = response;

        return context;
    }
}
