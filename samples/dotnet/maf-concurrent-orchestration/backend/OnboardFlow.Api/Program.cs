// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OnboardFlow.Api.Hubs;
using OnboardFlow.Application.Interfaces;
using OnboardFlow.Application.Orchestration;
using OnboardFlow.Application.Services;
using OnboardFlow.Infrastructure.Data;
using OnboardFlow.Infrastructure.LlmClients;
using OnboardFlow.Infrastructure.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Azure OpenAI configuration
string endpoint = builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is required");
string apiKey = builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey configuration is required");
string deployment = builder.Configuration["AzureOpenAI:ModelDeployment"] ?? "gpt-4o-mini";

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:4174")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// SQLite
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=onboardflow.db;Mode=ReadWriteCreate;Cache=Shared;Pooling=True;Timeout=30";
builder.Services.AddDbContext<OnboardFlowDbContext>(options =>
    options.UseSqlite(connectionString)
        .EnableDetailedErrors());

// IChatClient via Azure OpenAI
builder.Services.AddSingleton<IChatClient>(_ =>
{
    return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
        .GetChatClient(deployment)
        .AsIChatClient();
});

// Application services
builder.Services.AddScoped<ILlmClient, ChatClientLlmClient>();
builder.Services.AddScoped<IWorkflowRunRepository, WorkflowRunRepository>();
builder.Services.AddScoped<IWorkflowStepRunRepository, WorkflowStepRunRepository>();
builder.Services.AddScoped<IPiiRedactionService, PiiRedactionService>();
builder.Services.AddSingleton<IWorkflowProgressHub, WorkflowProgressHubService>();
builder.Services.AddSingleton<OnboardFlowOrchestrator>();

WebApplication app = builder.Build();

// Ensure database is created
using (IServiceScope scope = app.Services.CreateScope())
{
    OnboardFlowDbContext dbContext = scope.ServiceProvider.GetRequiredService<OnboardFlowDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

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
