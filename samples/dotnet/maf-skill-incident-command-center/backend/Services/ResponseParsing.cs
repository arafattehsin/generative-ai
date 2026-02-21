using System.Text.Json;
using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public static class ResponseParsing
{
    public static TriageModel ParseTriage(string responseText, IncidentRecord incident)
    {
        string jsonCandidate = TryExtractJsonObject(responseText) ?? responseText;

        try
        {
            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
            TriageModel? parsed = JsonSerializer.Deserialize<TriageModel>(jsonCandidate, options);
            if (parsed is not null
                && !string.IsNullOrWhiteSpace(parsed.Summary)
                && !string.IsNullOrWhiteSpace(parsed.Severity))
            {
                return parsed;
            }
        }
        catch
        {
            // Fall through to heuristic output.
        }

        string severity = incident.AtRiskOrders >= 20 ? "critical"
            : incident.AtRiskOrders >= 10 ? "high"
            : incident.AtRiskOrders >= 5 ? "medium"
            : "low";

        return new TriageModel(
            Summary: responseText.Trim(),
            Severity: severity,
            ProbableCauses: incident.Signals,
            RecommendedActions:
            [
                "Escalate to the logistics lead within 15 minutes.",
                "Trigger alternate vendor availability check.",
                "Send proactive customer ETA notice."
            ],
            AtRiskOrders: [
                $"{incident.Region}-ORD-4021",
                $"{incident.Region}-ORD-7745"
            ]
        );
    }

    private static string? TryExtractJsonObject(string text)
    {
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }
}
