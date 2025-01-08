using microsoft_agent_sk;
using Microsoft.Agents.Protocols.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using microsoft_agent_sk.Agents;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Register Semantic Kernel
builder.Services.AddKernel();

// Register the AI service of your choice. AzureOpenAI and OpenAI are demonstrated...
if (builder.Configuration.GetSection("Connections:BotServiceConnection:Settings:AIServices:AzureOpenAI").GetValue<bool>("UseAzureOpenAI"))
{
    var deploymentName = builder.Configuration.GetSection("Connections:BotServiceConnection:Settings:AIServices:AzureOpenAI").GetValue<string>("DeploymentName")
                         ?? throw new ArgumentNullException("Connections:BotServiceConnection:Settings:DeploymentName cannot be null");
    var endpoint = builder.Configuration.GetSection("Connections:BotServiceConnection:Settings:AIServices:AzureOpenAI").GetValue<string>("Endpoint")
                   ?? throw new ArgumentNullException("Endpoint cannot be null");
    var apiKey = builder.Configuration.GetSection("Connections:BotServiceConnection:Settings:AIServices:AzureOpenAI").GetValue<string>("ApiKey")
                 ?? throw new ArgumentNullException("ApiKey cannot be null");

    builder.Services.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: endpoint,
        apiKey: apiKey);
}

builder.Services.AddTransient<TravelAgent>();

builder.AddBot<IBot, BotHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => "Microsoft 365 Agent SDK");
    app.UseDeveloperExceptionPage();
    app.MapControllers().AllowAnonymous();
}
else
{
    app.MapControllers();
}
app.Run();