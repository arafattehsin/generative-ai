using System.Text.Json;
using IncidentCommandCenter.Api.Models;
using IncidentCommandCenter.Api.Services;

var builder = WebApplication.CreateBuilder(args);

string frontendOrigin = builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(frontendOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddSingleton<IIncidentRepository, FileIncidentRepository>();
builder.Services.AddSingleton<ISkillTraceStore, InMemorySkillTraceStore>();
builder.Services.AddSingleton<ISkillCatalogService, FileAgentSkillsProvider>();
builder.Services.AddSingleton<AgentRuntime>();
builder.Services.AddSingleton<ICommunicationDraftService, CommunicationDraftService>();

var app = builder.Build();

app.UseCors();

using (IServiceScope scope = app.Services.CreateScope())
{
    // Fail fast with a clear startup error if env vars are missing.
    scope.ServiceProvider.GetRequiredService<AgentRuntime>().ValidateConfiguration();
}

app.MapGet("/api/incidents", (IIncidentRepository repository) =>
{
    return Results.Ok(repository.GetAll());
});

app.MapPost("/api/sessions", async (AgentRuntime runtime, CancellationToken cancellationToken) =>
{
    string sessionId = await runtime.CreateSessionAsync(cancellationToken);
    return Results.Ok(new SessionResponse(sessionId));
});

app.MapGet("/api/skills", (ISkillCatalogService skillCatalog) =>
{
    return Results.Ok(skillCatalog.GetSkills());
});

app.MapGet("/api/runs/{runId}/events", (string runId, ISkillTraceStore traceStore) =>
{
    return Results.Ok(traceStore.GetEvents(runId));
});

app.MapPost("/api/triage", async (
    TriageRequest request,
    IIncidentRepository incidentRepository,
    ISkillCatalogService skillCatalog,
    ISkillTraceStore traceStore,
    AgentRuntime runtime,
    CancellationToken cancellationToken) =>
{
    if (!runtime.SessionExists(request.SessionId))
    {
        return Results.BadRequest(new ErrorResponse("invalid_session", "The provided sessionId does not exist."));
    }

    IncidentRecord? incident = incidentRepository.GetById(request.IncidentId);
    if (incident is null)
    {
        return Results.NotFound(new ErrorResponse("incident_not_found", "The incidentId was not found in local data."));
    }

    string runId = $"run-{Guid.NewGuid():N}";

    IReadOnlyList<SkillSummary> advertisedSkills = skillCatalog.GetSkills();
    foreach (SkillSummary skill in advertisedSkills)
    {
        traceStore.AddEvent(SkillEventFactory.Create(runId, "advertised", skill.Name, null,
            "Skill advertised to agent via context provider."));
    }

    traceStore.AddEvent(SkillEventFactory.Create(runId, "loaded", "incident-triage", null,
        "Loaded triage instructions."));

    IReadOnlyList<string> triageResources = skillCatalog.GetResources("incident-triage");
    foreach (string resourcePath in triageResources)
    {
        traceStore.AddEvent(SkillEventFactory.Create(runId, "resource_read", "incident-triage", resourcePath,
            "Loaded triage resource."));
    }

    string resourcesBundle = string.Join(
        "\n\n",
        triageResources.Select(path => $"### {path}\n{skillCatalog.ReadResource("incident-triage", path)}"));

    string triagePrompt = $$"""
You are triaging a supply chain incident.
Use the provided skill resources and output STRICT JSON only with this schema:
{
  "summary": string,
  "severity": "low" | "medium" | "high" | "critical",
  "probableCauses": string[],
  "recommendedActions": string[],
  "atRiskOrders": string[]
}

Incident payload:
{{JsonSerializer.Serialize(incident)}}

Operator prompt:
{{request.UserPrompt ?? "Classify severity and provide an action plan."}}

Skill resources:
{{resourcesBundle}}
""";

    string rawResponse = await runtime.RunAsync(request.SessionId, triagePrompt, cancellationToken);
    TriageModel triage = ResponseParsing.ParseTriage(rawResponse, incident);

    traceStore.AddEvent(SkillEventFactory.Create(runId, "completed", "incident-triage", null,
        "Triage completed."));

    TriageResponse response = new(
        RunId: runId,
        Summary: triage.Summary,
        Severity: triage.Severity.ToLowerInvariant(),
        ProbableCauses: triage.ProbableCauses,
        RecommendedActions: triage.RecommendedActions,
        AtRiskOrders: triage.AtRiskOrders
    );

    return Results.Ok(response);
});

app.MapPost("/api/communications/draft", async (
    CommunicationDraftRequest request,
    IIncidentRepository incidentRepository,
    ISkillCatalogService skillCatalog,
    ISkillTraceStore traceStore,
    ICommunicationDraftService communicationService,
    AgentRuntime runtime,
    CancellationToken cancellationToken) =>
{
    if (!runtime.SessionExists(request.SessionId))
    {
        return Results.BadRequest(new ErrorResponse("invalid_session", "The provided sessionId does not exist."));
    }

    IncidentRecord? incident = incidentRepository.GetById(request.IncidentId);
    if (incident is null)
    {
        return Results.NotFound(new ErrorResponse("incident_not_found", "The incidentId was not found in local data."));
    }

    string runId = $"run-{Guid.NewGuid():N}";

    IReadOnlyList<SkillSummary> advertisedSkills = skillCatalog.GetSkills();
    foreach (SkillSummary skill in advertisedSkills)
    {
        traceStore.AddEvent(SkillEventFactory.Create(runId, "advertised", skill.Name, null,
            "Skill advertised to agent via context provider."));
    }

    CommunicationDraftResponse response = await communicationService.DraftAsync(
        request.SessionId,
        runId,
        incident,
        request.Audience,
        request.TriageSummary,
        cancellationToken);

    return Results.Ok(response);
});

app.Run();

public partial class Program;
