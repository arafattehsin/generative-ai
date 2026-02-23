using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public sealed class CommunicationDraftService(AgentRuntime runtime) : ICommunicationDraftService
{
    private static readonly string[] SupportedAudiences = ["customer", "supplier", "internal-leadership"];

    public async Task<CommunicationDraftResponse> DraftAsync(
        string sessionId,
        string runId,
        IncidentRecord incident,
        string audience,
        string? triageSummary,
        CancellationToken cancellationToken = default)
    {
        string normalizedAudience = NormalizeAudience(audience);

        string prompt = $$"""
You are preparing an urgent disruption communication.
Use native Agent Skills invocation.
First load the `incident-communications` skill with load_skill, then read the relevant resources with read_skill_resource.

Audience: {{normalizedAudience}}

Incident data:
{{System.Text.Json.JsonSerializer.Serialize(incident)}}

Optional triage summary:
{{triageSummary ?? "No triage summary was supplied."}}

Output only the final message body as markdown.
""";

        string draft = await runtime.RunAsync(sessionId, prompt, runId, cancellationToken);

        return new CommunicationDraftResponse(runId, normalizedAudience, draft.Trim());
    }

    private static string NormalizeAudience(string audience)
    {
        string normalized = audience.Trim().ToLowerInvariant();
        if (SupportedAudiences.Contains(normalized, StringComparer.Ordinal))
        {
            return normalized;
        }

        return "customer";
    }
}
