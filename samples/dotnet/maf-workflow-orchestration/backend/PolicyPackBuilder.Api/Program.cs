// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using PolicyPackBuilder.Api.Hubs;
using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Application.Orchestration;
using PolicyPackBuilder.Application.Services;
using PolicyPackBuilder.Infrastructure.Data;
using PolicyPackBuilder.Infrastructure.LlmClients;
using PolicyPackBuilder.Infrastructure.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration - Azure OpenAI (loaded from appsettings.json or appsettings.Development.json)
string azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is required");
string azureOpenAIKey = builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey configuration is required");
string modelDeployment = builder.Configuration["AzureOpenAI:ModelDeployment"] ?? "gpt-4o-mini";

Console.WriteLine($"[CONFIG] Azure OpenAI Endpoint: {azureOpenAIEndpoint}");
Console.WriteLine($"[CONFIG] Model: {modelDeployment}");

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add SQLite DbContext
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=policypack.db;Mode=ReadWriteCreate;Cache=Shared;Pooling=True;Timeout=30";
builder.Services.AddDbContext<PolicyPackDbContext>(options =>
    options.UseSqlite(connectionString)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors());

// Add IChatClient using Azure OpenAI with API key
builder.Services.AddSingleton<IChatClient>(_ =>
{
    return new AzureOpenAIClient(new Uri(azureOpenAIEndpoint), new ApiKeyCredential(azureOpenAIKey))
        .GetChatClient(modelDeployment)
        .AsIChatClient();
});

// Register application services
builder.Services.AddScoped<ILlmClient, ChatClientLlmClient>();
builder.Services.AddScoped<IWorkflowRunRepository, WorkflowRunRepository>();
builder.Services.AddScoped<IWorkflowStepRunRepository, WorkflowStepRunRepository>();
builder.Services.AddScoped<IPiiRedactionService, PiiRedactionService>();
builder.Services.AddScoped<IComplianceRulesEngine, ComplianceRulesEngine>();
builder.Services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();
builder.Services.AddScoped<IWorkflowProgressHub, WorkflowProgressHubService>();

WebApplication app = builder.Build();

// Ensure database is created
using (IServiceScope scope = app.Services.CreateScope())
{
    PolicyPackDbContext dbContext = scope.ServiceProvider.GetRequiredService<PolicyPackDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RunsHub>("/hubs/runs");

app.Run();
