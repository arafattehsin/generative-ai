using Microsoft.AspNetCore.SignalR;

namespace CivicWorks.Api.Hubs;

public sealed class CivicWorksHub : Hub
{
    public Task JoinRun(string runId) => Groups.AddToGroupAsync(Context.ConnectionId, RunGroup(runId));

    public Task LeaveRun(string runId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, RunGroup(runId));

    internal static string RunGroup(Guid runId) => $"civicworks-run-{runId:N}";
    private static string RunGroup(string runId) => Guid.TryParse(runId, out Guid id) ? RunGroup(id) : "invalid-run";
}
