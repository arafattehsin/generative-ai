using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public interface ICommunicationDraftService
{
    Task<CommunicationDraftResponse> DraftAsync(
        string sessionId,
        string runId,
        IncidentRecord incident,
        string audience,
        string? triageSummary,
        CancellationToken cancellationToken = default);
}
