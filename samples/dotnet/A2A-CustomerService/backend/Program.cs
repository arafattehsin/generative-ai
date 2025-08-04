using A2A;
using A2A.AspNetCore;
using A2ACustomerService.Configuration;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// ⭐ CRITICAL A2A PROTOCOL COMPLIANCE: Expose agent via A2A protocol
var taskManager = app.Services.GetRequiredService<ITaskManager>();
app.MapA2A(taskManager, "/agent");

// Add A2A agent discovery endpoint
app.MapGet("/api/a2a/agents", (ITaskManager taskManager) =>
{
    return Results.Ok(new { agents = new[] { "customer-service-agent" } });
});

// Add a simple health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run();
