using Microsoft.AspNetCore.SignalR;
using TravelConcierge.Api.Hubs;

namespace TravelConcierge.Api.Services;

public sealed class RunEventNotifier(IHubContext<RunsHub> hubContext, RunStore store)
{
    public async Task NotifyRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        TravelConcierge.Api.Models.TravelRun? run = store.Get(runId);
        if (run == null)
        {
            return;
        }

        await hubContext.Clients
            .Group(RunsHub.RunGroup(runId.ToString()))
            .SendAsync("runUpdated", run, cancellationToken)
            .ConfigureAwait(false);
    }
}
