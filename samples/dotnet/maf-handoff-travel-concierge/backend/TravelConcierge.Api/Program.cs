using System.Text.Json.Serialization;
using TravelConcierge.Api.Hubs;
using TravelConcierge.Api.Models;
using TravelConcierge.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174",
                "http://localhost:5175",
                "http://127.0.0.1:5175")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

FoundryOptions foundryOptions = FoundryOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(foundryOptions);
builder.Services.AddSingleton<RunStore>();
builder.Services.AddSingleton<SampleDataService>();
builder.Services.AddSingleton<RunEventNotifier>();
builder.Services.AddSingleton<HumanSupportWorkflowService>();
builder.Services.AddSingleton<FoundryCredentialProvider>();
builder.Services.AddSingleton<FoundryHandoffRunner>();
builder.Services.AddSingleton<RunCoordinator>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.MapHub<RunsHub>("/hubs/runs");

app.MapGet("/", () => Results.Json(new
{
    name = "Travel Disruption Concierge",
    description = "Microsoft Agent Framework handoff orchestration sample.",
    api = "/swagger",
}));

app.MapGet("/api/config", (FoundryOptions options) => Results.Json(new ConfigResponse(
    HasProjectEndpoint: Uri.TryCreate(options.ProjectEndpoint, UriKind.Absolute, out _),
    ProjectEndpoint: options.ProjectEndpoint,
    DeploymentName: options.DeploymentName,
    Agents: TravelAgents.All)));

app.Run();
