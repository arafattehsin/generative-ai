using Microsoft.AspNetCore.SignalR;

namespace TravelConcierge.Api.Hubs;

public sealed class RunsHub : Hub
{
    public Task JoinRun(string runId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, RunGroup(runId));

    public Task LeaveRun(string runId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, RunGroup(runId));

    public static string RunGroup(string runId) => $"run:{runId}";
}
