using System.Text.Json;
using CivicWorks.Api.Models;

namespace CivicWorks.Api.Services;

public static class PreliminaryWorksOptionsBriefParser
{
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public static PreliminaryWorksOptionsBrief Parse(string modelOutput)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
        {
            throw new InvalidOperationException("The live Magentic manager returned an empty final answer.");
        }

        string json = ExtractJson(modelOutput);
        PreliminaryWorksOptionsBrief? brief;

        try
        {
            brief = JsonSerializer.Deserialize<PreliminaryWorksOptionsBrief>(json, s_options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "The live Magentic manager did not return the required PreliminaryWorksOptionsBrief JSON. " +
                "CivicWorks will not substitute a scripted brief.",
                ex);
        }

        if (brief is null || string.IsNullOrWhiteSpace(brief.Recommendation) || string.IsNullOrWhiteSpace(brief.Summary))
        {
            throw new InvalidOperationException("The live brief is missing its recommendation or summary.");
        }

        string[] codes = brief.Options.Select(option => option.Code.ToUpperInvariant()).Order().ToArray();
        if (!codes.SequenceEqual(["A", "B", "C"], StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The live brief must contain exactly options A, B, and C.");
        }

        if (!brief.Options.Any(option =>
                string.Equals(option.Code, brief.Recommendation, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The live recommendation must identify one of the returned options.");
        }

        if (brief.Claims.Count == 0 || brief.Claims.Any(claim => claim.EvidenceReferences.Count == 0))
        {
            throw new InvalidOperationException("Every material claim in the live brief must retain evidence references.");
        }

        return brief;
    }

    private static string ExtractJson(string output)
    {
        string trimmed = output.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNewLine = trimmed.IndexOf('\n');
            int closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && closingFence > firstNewLine)
            {
                return trimmed[(firstNewLine + 1)..closingFence].Trim();
            }
        }

        int firstBrace = trimmed.IndexOf('{');
        int lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return trimmed[firstBrace..(lastBrace + 1)];
        }

        return trimmed;
    }
}
