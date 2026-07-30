using TravelConcierge.Api.Models;

namespace TravelConcierge.Api.Services;

public interface ITravelConciergeRunner
{
    Task ProcessTurnAsync(TravelRun run, string userMessage, RunEventWriter writer, CancellationToken cancellationToken);
}
