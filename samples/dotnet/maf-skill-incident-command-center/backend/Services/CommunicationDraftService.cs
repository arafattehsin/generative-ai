using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public sealed class CommunicationDraftService(
    AgentRuntime runtime,
    ISkillCatalogService skillCatalog,
    ISkillTraceStore traceStore) : ICommunicationDraftService
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

        traceStore.AddEvent(SkillEventFactory.Create(runId, "loaded", "incident-communications", null,
            $"Loaded communication skill for audience '{normalizedAudience}'."));

        foreach (string resourcePath in skillCatalog.GetResources("incident-communications"))
        {
            traceStore.AddEvent(SkillEventFactory.Create(runId, "resource_read", "incident-communications", resourcePath,
                "Loaded communication resource."));
        }

        string toneGuidelines = skillCatalog.ReadResource("incident-communications", "references/tone-guidelines.md");
        string templatePath = normalizedAudience switch
        {
            "supplier" => "assets/supplier-escalation-template.md",
            _ => "assets/customer-update-template.md"
        };
        string template = skillCatalog.ReadResource("incident-communications", templatePath);

        string prompt = $$"""
You are preparing an urgent disruption communication.
Use this audience: {{normalizedAudience}}.

Incident data:
{{System.Text.Json.JsonSerializer.Serialize(incident)}}

Optional triage summary:
{{triageSummary ?? "No triage summary was supplied."}}

Tone and rules:
{{toneGuidelines}}

Use this template and keep placeholders resolved:
{{template}}

Output only the final message body as markdown.
""";

        string draft = await runtime.RunAsync(sessionId, prompt, cancellationToken);

        traceStore.AddEvent(SkillEventFactory.Create(runId, "completed", "incident-communications", null,
            "Draft generated."));

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
