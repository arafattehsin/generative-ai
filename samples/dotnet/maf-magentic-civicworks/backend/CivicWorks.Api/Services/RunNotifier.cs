using CivicWorks.Api.Hubs;
using CivicWorks.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace CivicWorks.Api.Services;

public sealed class RunNotifier(
    CivicWorksRunStore store,
    IHubContext<CivicWorksHub> hubContext)
{
    public async Task PublishAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (!store.TryGet(runId, out CivicWorksRunSnapshot? snapshot) || snapshot is null)
        {
            return;
        }

        await hubContext.Clients
            .Group(CivicWorksHub.RunGroup(runId))
            .SendAsync("runUpdated", snapshot, cancellationToken)
            .ConfigureAwait(false);
    }
}
