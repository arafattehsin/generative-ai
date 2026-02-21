using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public interface IIncidentRepository
{
    IReadOnlyList<IncidentRecord> GetAll();
    IncidentRecord? GetById(string incidentId);
}
