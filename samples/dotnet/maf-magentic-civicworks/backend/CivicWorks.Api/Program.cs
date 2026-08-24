using System.Text.Json.Serialization;
using CivicWorks.Api.Hubs;
using CivicWorks.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddControllers().AddJsonOptions(options =>
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
                "http://localhost:4177",
                "http://127.0.0.1:4177")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

FoundryOptions foundryOptions = FoundryOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(foundryOptions);
builder.Services.AddSingleton<CivicWorksRunStore>();
builder.Services.AddSingleton<RunNotifier>();
builder.Services.AddSingleton<LiveMagenticRunService>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.MapHub<CivicWorksHub>("/hubs/civicworks");

app.MapGet("/", () => Results.Json(new
{
    name = "CivicWorks",
    description = "Live-only Microsoft Agent Framework Magentic orchestration sample.",
    simulationFallback = false,
    api = "/swagger",
}));

app.MapGet("/api/config", (FoundryOptions options) => Results.Json(options.GetStatus()));

app.Run();

public partial class Program;
