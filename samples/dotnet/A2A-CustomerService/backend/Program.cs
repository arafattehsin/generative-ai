using A2A;
using A2A.AspNetCore;
using A2ACustomerService.Configuration;
using A2ACustomerService.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
// Swagger/OpenAPI setup removed for .NET 9 compatibility

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add A2A services
builder.Services.AddA2AServices(builder.Configuration);

// Add logging
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Swagger/OpenAPI middleware removed for .NET 9 compatibility
}

app.UseCors("AllowReactApp");
app.UseRouting();
app.UseAuthorization();

app.MapControllers();





var frontDeskAgent = app.Services.GetRequiredService<FrontDeskAgent>();
var billingAgent = app.Services.GetRequiredService<BillingAgent>();
var technicalAgent = app.Services.GetRequiredService<TechnicalAgent>();

var frontDeskTaskManager = new TaskManager();
frontDeskAgent.Attach(frontDeskTaskManager);
app.MapA2A(frontDeskTaskManager, "/frontdesk");

var billingTaskManager = new TaskManager();
billingAgent.Attach(billingTaskManager);
app.MapA2A(billingTaskManager, "/billing");

var technicalTaskManager = new TaskManager();
technicalAgent.Attach(technicalTaskManager);
app.MapA2A(technicalTaskManager, "/technical");

var orchestratorAgent = app.Services.GetRequiredService<OrchestratorAgent>();

var orchestratorTaskManager = new TaskManager();
orchestratorAgent.Attach(orchestratorTaskManager);
app.MapA2A(orchestratorTaskManager, "/orchestrator");

// Add A2A agent discovery endpoint
app.MapGet("/api/a2a/agents", () =>
{
    return Results.Ok(new { agents = new[] { "frontdesk", "billing", "technical", "orchestrator" } });
});

// Add a simple health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run();
