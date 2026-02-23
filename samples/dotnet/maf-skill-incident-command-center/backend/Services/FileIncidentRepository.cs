using System.Text.Json;
using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public sealed class FileIncidentRepository : IIncidentRepository
{
    private readonly string _incidentDirectory;
    private readonly Lazy<IReadOnlyList<IncidentRecord>> _cache;

    public FileIncidentRepository(IHostEnvironment environment)
    {
        _incidentDirectory = Path.Combine(environment.ContentRootPath, "data", "incidents");
        _cache = new Lazy<IReadOnlyList<IncidentRecord>>(LoadIncidents);
    }

    public IReadOnlyList<IncidentRecord> GetAll() => _cache.Value;

    public IncidentRecord? GetById(string incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId))
        {
            return null;
        }

        return _cache.Value.FirstOrDefault(i =>
            string.Equals(i.Id, incidentId, StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<IncidentRecord> LoadIncidents()
    {
        if (!Directory.Exists(_incidentDirectory))
        {
            return [];
        }

        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        List<IncidentRecord> incidents = [];

        foreach (string filePath in Directory.GetFiles(_incidentDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            string json = File.ReadAllText(filePath);
            IncidentRecord? incident = JsonSerializer.Deserialize<IncidentRecord>(json, options);
            if (incident is not null)
            {
                incidents.Add(incident);
            }
        }

        return incidents
            .OrderByDescending(i => i.AtRiskOrders)
            .ThenBy(i => i.SlaDeadlineUtc)
            .ToArray();
    }
}
