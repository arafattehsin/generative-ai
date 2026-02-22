using System.Diagnostics;
using System.Text.Json;
using IncidentCommandCenter.Api.Models;
using IncidentCommandCenter.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss.fff ";
});

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
builder.Services.AddSingleton<SkillRunContextAccessor>();
builder.Services.AddSingleton<AgentRuntime>();
builder.Services.AddSingleton<ICommunicationDraftService, CommunicationDraftService>();

var app = builder.Build();

app.UseCors();
app.Use(async (context, next) =>
{
    Stopwatch timer = Stopwatch.StartNew();
    app.Logger.LogInformation("HTTP {Method} {Path} started", context.Request.Method, context.Request.Path);
    await next();
    timer.Stop();
    app.Logger.LogInformation(
        "HTTP {Method} {Path} completed {StatusCode} in {ElapsedMs}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        timer.ElapsedMilliseconds);
});

using (IServiceScope scope = app.Services.CreateScope())
{
    // Fail fast with a clear startup error if env vars are missing.
    scope.ServiceProvider.GetRequiredService<AgentRuntime>().ValidateConfiguration();
}

app.MapGet("/api/incidents", (IIncidentRepository repository, ILogger<Program> logger) =>
{
    logger.LogInformation("Fetching incident list.");
    return Results.Ok(repository.GetAll());
});

app.MapPost("/api/sessions", async (AgentRuntime runtime, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    string sessionId = await runtime.CreateSessionAsync(cancellationToken);
    logger.LogInformation("Created session {SessionId}", sessionId);
    return Results.Ok(new SessionResponse(sessionId));
});

app.MapGet("/api/skills", (ISkillCatalogService skillCatalog, ILogger<Program> logger) =>
{
    IReadOnlyList<SkillSummary> skills = skillCatalog.GetSkills();
    logger.LogInformation("Returning advertised skills. Count={SkillCount}", skills.Count);
    return Results.Ok(skills);
});

app.MapGet("/api/runs/{runId}/events", (string runId, ISkillTraceStore traceStore, ILogger<Program> logger) =>
{
    IReadOnlyList<SkillEvent> events = traceStore.GetEvents(runId);
    logger.LogInformation("Returning run events. RunId={RunId}, EventCount={EventCount}", runId, events.Count);
    return Results.Ok(events);
});

app.MapPost("/api/triage", async (
    TriageRequest request,
    IIncidentRepository incidentRepository,
    ISkillCatalogService skillCatalog,
    ISkillTraceStore traceStore,
    AgentRuntime runtime,
    ILogger<Program> logger,
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
    logger.LogInformation(
        "Starting triage API flow. RunId={RunId}, SessionId={SessionId}, IncidentId={IncidentId}",
        runId,
        request.SessionId,
        request.IncidentId);

    IReadOnlyList<SkillSummary> advertisedSkills = skillCatalog.GetSkills();
    foreach (SkillSummary skill in advertisedSkills)
    {
        traceStore.AddEvent(SkillEventFactory.Create(runId, "advertised", skill.Name, null,
            "Skill advertised to agent via context provider."));
    }

    string triagePrompt = $$"""
You are triaging a supply chain incident.
Use native Agent Skills invocation.
First load the `incident-triage` skill using the skill tools, then read the relevant references/assets needed for your reasoning.
After using those resources, output STRICT JSON only with this schema:
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
""";

    string rawResponse = await runtime.RunAsync(request.SessionId, triagePrompt, runId, cancellationToken);
    IReadOnlyList<SkillEvent> runEvents = traceStore.GetEvents(runId);
    if (!SkillExecutionGuard.HasLoadedSkill(runEvents, "incident-triage"))
    {
        logger.LogWarning(
            "Native skill invocation missing for triage run. RunId={RunId}, SessionId={SessionId}",
            runId,
            request.SessionId);
        return Results.BadRequest(new ErrorResponse(
            "skill_invocation_failed",
            "Expected native skill invocation did not occur for 'incident-triage'. Ensure the model is allowed to call load_skill/read_skill_resource."));
    }

    TriageModel triage = ResponseParsing.ParseTriage(rawResponse, incident);

    traceStore.AddEvent(SkillEventFactory.Create(runId, "completed", "incident-triage", null,
        "Triage completed."));
    logger.LogInformation(
        "Completed triage API flow. RunId={RunId}, Severity={Severity}",
        runId,
        triage.Severity);

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
    ILogger<Program> logger,
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
    logger.LogInformation(
        "Starting communication draft API flow. RunId={RunId}, SessionId={SessionId}, IncidentId={IncidentId}, Audience={Audience}",
        runId,
        request.SessionId,
        request.IncidentId,
        request.Audience);

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

    IReadOnlyList<SkillEvent> runEvents = traceStore.GetEvents(runId);
    if (!SkillExecutionGuard.HasLoadedSkill(runEvents, "incident-communications"))
    {
        logger.LogWarning(
            "Native skill invocation missing for communication run. RunId={RunId}, SessionId={SessionId}, Audience={Audience}",
            runId,
            request.SessionId,
            request.Audience);
        return Results.BadRequest(new ErrorResponse(
            "skill_invocation_failed",
            "Expected native skill invocation did not occur for 'incident-communications'. Ensure the model is allowed to call load_skill/read_skill_resource."));
    }

    traceStore.AddEvent(SkillEventFactory.Create(runId, "completed", "incident-communications", null,
        "Communication draft completed."));
    logger.LogInformation("Completed communication draft API flow. RunId={RunId}", runId);

    return Results.Ok(response);
});

app.Run();

public partial class Program;
