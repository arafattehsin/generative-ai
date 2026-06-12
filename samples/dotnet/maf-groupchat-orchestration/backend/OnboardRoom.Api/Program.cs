// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OnboardRoom.Api.Hubs;
using OnboardRoom.Api.Services;
using OnboardRoom.Application.Interfaces;
using OnboardRoom.Application.Services;
using OnboardRoom.Infrastructure.Data;
using OnboardRoom.Infrastructure.Foundry;
using OnboardRoom.Infrastructure.Repositories;
using OnboardRoom.Infrastructure.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
        policy.WithOrigins("http://localhost:5173", "http://localhost:5178", "http://localhost:3000", "http://127.0.0.1:5178", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=onboardroom.db;Mode=ReadWriteCreate;Cache=Shared;Pooling=True";

builder.Services.AddDbContext<OnboardRoomDbContext>(options =>
    options.UseSqlite(connectionString).EnableDetailedErrors());

FoundryOptions foundryOptions = builder.Configuration.GetSection("Foundry").Get<FoundryOptions>() ?? new FoundryOptions();
foundryOptions.ProjectEndpoint = FirstConfigured(
    builder.Configuration["FOUNDRY_PROJECT_ENDPOINT"],
    builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"],
    foundryOptions.ProjectEndpoint);
foundryOptions.DeploymentName = FirstConfigured(
    builder.Configuration["FOUNDRY_MODEL"],
    builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"],
    foundryOptions.DeploymentName);
foundryOptions.ToolboxName = FirstConfigured(builder.Configuration["FOUNDRY_TOOLBOX_NAME"], foundryOptions.ToolboxName);
foundryOptions.ToolboxApiVersion = FirstConfigured(
    builder.Configuration["FOUNDRY_TOOLBOX_API_VERSION"],
    builder.Configuration["FOUNDRY_AGENT_TOOLSET_API_VERSION"],
    foundryOptions.ToolboxApiVersion);

builder.Services.AddSingleton(foundryOptions);
builder.Services.AddSingleton<FoundryAgentFactory>();
builder.Services.AddScoped<IGroupChatWorkflowRunner, FoundryGroupChatWorkflowRunner>();
builder.Services.AddScoped<IRunRepository, EfRunRepository>();
builder.Services.AddScoped<IPiiRedactor, RegexPiiRedactor>();
builder.Services.AddScoped<IRunEventSink, SignalRRunEventSink>();
builder.Services.AddScoped<RunOrchestrator>();
builder.Services.AddSingleton<SampleDataService>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    OnboardRoomDbContext dbContext = scope.ServiceProvider.GetRequiredService<OnboardRoomDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RunsHub>("/hubs/runs");

app.MapPost("/responses", () => Results.Json(new
{
    message = "This sample exposes Foundry through Microsoft Agent Framework workflows. Host this API behind a Foundry Responses app and map the toolboxes with AddFoundryToolboxes(...) for production-hosted response routing.",
}));

app.Run();

static string FirstConfigured(params string?[] values)
    => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
