// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OnboardFlow.Application.Interfaces;

namespace OnboardFlow.Application.Executors.Maf;

/// <summary>
/// MAF fan-in aggregation executor. Receives review results from all concurrent
/// reviewer agents (after the barrier releases), merges them into a DecisionPack
/// JSON, and yields the result as the workflow output.
/// </summary>
internal sealed class ReviewAggregationExecutor : Executor<List<ChatMessage>>
{
    private readonly ILlmClient _llmClient;
    private readonly List<(string Agent, string ReviewJson)> _reviews = [];
    private const int ExpectedReviewerCount = 3;

    public ReviewAggregationExecutor(ILlmClient llmClient)
        : base("ReviewAggregation")
    {
        _llmClient = llmClient;
    }

    public override async ValueTask HandleAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        // Each invocation delivers the aggregated turn messages from one reviewer agent.
        var assistantMessage = messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
        if (assistantMessage != null)
        {
            _reviews.Add((assistantMessage.AuthorName ?? "Unknown", assistantMessage.Text ?? ""));
        }

        if (_reviews.Count >= ExpectedReviewerCount)
        {
            string decisionPackJson = await MergeReviewsAsync(ct);
            await context.YieldOutputAsync(decisionPackJson, ct);
        }
    }

    private async Task<string> MergeReviewsAsync(CancellationToken ct)
    {
        string systemMessage = """
            You are a senior decision aggregator. You receive review assessments from
            Security, Compliance, and Finance reviewers. Merge them into a single
            Decision Pack JSON object.

            The output MUST be a valid JSON object with this schema:
            {
              "overallRecommendation": "Approve | Approve with Conditions | Reject",
              "conditions": ["string"],
              "mergedFindings": [
                {
                  "category": "Security | Compliance | Finance",
                  "title": "string",
                  "detail": "string",
                  "severity": "Low | Medium | High | Critical",
                  "sources": ["SecurityReviewer", "ComplianceReviewer", "FinanceReviewer"]
                }
              ],
              "conflicts": [
                {
                  "topic": "string",
                  "opinions": [
                    { "agent": "string", "text": "string" }
                  ]
                }
              ],
              "requiredNextActions": ["string"],
              "warnings": ["string"]
            }

            Rules:
            - Identify conflicting opinions across reviewers and list them explicitly.
            - Merge overlapping findings from different reviewers.
            - The overall recommendation should be the most restrictive one.
            - Return ONLY the JSON object, no markdown or explanation.
            """;

        string allReviews = string.Join("\n\n", _reviews.Select(r =>
            $"--- {r.Agent} ---\n{r.ReviewJson}\n--- END {r.Agent} ---"));

        string prompt = $"""
            Merge the following reviewer assessments into a single Decision Pack:

            {allReviews}
            """;

        return await _llmClient.InvokeForJsonAsync(prompt, systemMessage, ct);
    }
}
