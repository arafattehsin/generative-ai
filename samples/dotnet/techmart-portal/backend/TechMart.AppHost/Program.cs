// Copyright (c) Microsoft. All rights reserved.

// TechMart Enterprise Operations Portal - Aspire AppHost
// Orchestrates all microservices for the retail operations platform

var builder = DistributedApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════
// Configuration from user secrets or environment variables
// ═══════════════════════════════════════════════════════════════════

string azureOpenAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]
    ?? builder.Configuration["AOI_ENDPOINT_SWDN"]
    ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured. Set AzureOpenAI:Endpoint in user secrets or AOI_ENDPOINT_SWDN environment variable.");

string? azureOpenAiKey = builder.Configuration["AzureOpenAI:Key"]
    ?? builder.Configuration["AOI_KEY_SWDN"];

string deploymentName = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-5.1";

// Tavily API Key for web search (optional)
string? tavilyApiKey = builder.Configuration["Tavily:ApiKey"]
    ?? builder.Configuration["TAVILY_API_KEY"]
    ?? Environment.GetEnvironmentVariable("TAVILY_API_KEY");

// Create connection string (with optional API key)
string connectionString = string.IsNullOrEmpty(azureOpenAiKey)
    ? $"Endpoint={azureOpenAiEndpoint};Deployment={deploymentName};Provider=AzureOpenAI"
    : $"Endpoint={azureOpenAiEndpoint};Key={azureOpenAiKey};Deployment={deploymentName};Provider=AzureOpenAI";

// ═══════════════════════════════════════════════════════════════════
// Remote A2A Agent Services
// ═══════════════════════════════════════════════════════════════════

// Inventory Agent - handles stock levels, warehouse operations
var inventoryAgent = builder.AddProject<Projects.TechMart_InventoryAgent>("inventory-agent")
    .WithEnvironment("ConnectionStrings__chat-model", connectionString);

// Finance Agent - handles pricing, quotes, payments
var financeAgent = builder.AddProject<Projects.TechMart_FinanceAgent>("finance-agent")
    .WithEnvironment("ConnectionStrings__chat-model", connectionString);

// Support Agent - handles customer support escalations
var supportAgent = builder.AddProject<Projects.TechMart_SupportAgent>("support-agent")
    .WithEnvironment("ConnectionStrings__chat-model", connectionString);

// ═══════════════════════════════════════════════════════════════════
// Main Agent Host Service
// ═══════════════════════════════════════════════════════════════════

var agentHost = builder.AddProject<Projects.TechMart_AgentHost>("agent-host")
    .WithHttpEndpoint(name: "devui")
    .WithUrlForEndpoint("devui", url => new() { Url = "/devui", DisplayText = "TechMart DevUI" })
    .WithEnvironment("ConnectionStrings__chat-model", connectionString)
    .WithEnvironment("TAVILY_API_KEY", tavilyApiKey ?? "")
    .WithReference(inventoryAgent)
    .WithReference(financeAgent)
    .WithReference(supportAgent)
    .WaitFor(inventoryAgent)
    .WaitFor(financeAgent)
    .WaitFor(supportAgent);

// ═══════════════════════════════════════════════════════════════════
// Build and Run
// ═══════════════════════════════════════════════════════════════════

builder.Build().Run();
