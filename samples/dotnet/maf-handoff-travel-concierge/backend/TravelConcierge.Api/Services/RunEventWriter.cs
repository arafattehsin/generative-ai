using TravelConcierge.Api.Models;

namespace TravelConcierge.Api.Services;

public sealed class RunEventWriter(RunStore store, RunEventNotifier notifier, Guid runId)
{
    public async Task SetStatusAsync(RunStatus status, string? summary = null, CancellationToken cancellationToken = default)
    {
        store.SetStatus(runId, status, summary);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddSystemMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        store.AddSystemMessage(runId, text);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid> StartAgentMessageAsync(AgentDefinition agent, CancellationToken cancellationToken = default)
    {
        Guid messageId = store.AddAgentMessage(runId, agent);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
        return messageId;
    }

    public async Task AddAgentMessageAsync(AgentDefinition agent, string text, CancellationToken cancellationToken = default)
    {
        store.AddAgentMessage(runId, agent, text);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddHumanMessageAsync(string operatorName, string text, CancellationToken cancellationToken = default)
    {
        store.AddHumanMessage(runId, operatorName, text);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task AppendAgentMessageAsync(Guid messageId, string text, CancellationToken cancellationToken = default)
    {
        store.AppendAgentMessage(runId, messageId, text);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandoffAsync(AgentDefinition from, AgentDefinition to, string reason, CancellationToken cancellationToken = default)
    {
        store.AddHandoff(runId, from, to, reason);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetOwnerAsync(AgentDefinition agent, CancellationToken cancellationToken = default)
    {
        store.SetOwner(runId, agent);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task RequestHumanSupportAsync(string requestId, HumanSupportRequest request, CancellationToken cancellationToken = default)
    {
        store.RequestHumanSupport(runId, requestId, request);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResolveHumanSupportAsync(HumanSupportResolution resolution, CancellationToken cancellationToken = default)
    {
        store.ResolveHumanSupport(runId, resolution);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateCaseAsync(TravelCaseSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        store.UpdateCase(runId, snapshot);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
    }
}
