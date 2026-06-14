// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.SignalR;

namespace OnboardRoom.Api.Hubs;

public sealed class RunsHub : Hub
{
    public async Task JoinRun(string runId)
        => await this.Groups.AddToGroupAsync(this.Context.ConnectionId, runId);

    public async Task LeaveRun(string runId)
        => await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, runId);
}
