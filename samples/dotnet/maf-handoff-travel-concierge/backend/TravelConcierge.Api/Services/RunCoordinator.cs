using System.Collections.Concurrent;
using TravelConcierge.Api.Models;

namespace TravelConcierge.Api.Services;

public sealed class RunCoordinator(
    RunStore store,
    RunEventNotifier notifier,
    FoundryHandoffRunner foundryRunner,
    HumanSupportWorkflowService humanSupport,
    ILogger<RunCoordinator> logger)
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<Guid> StartRunAsync(CreateRunRequest request, CancellationToken cancellationToken)
    {
        TravelRun run = store.Create(request);
        await notifier.NotifyRunAsync(run.Id, cancellationToken).ConfigureAwait(false);

        _ = Task.Run(() => ProcessTurnSafelyAsync(run.Id, request.Message, CancellationToken.None), CancellationToken.None);
        return run.Id;
    }

    public async Task<bool> SendMessageAsync(Guid runId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!store.TryGet(runId, out TravelRun run) ||
            run.Status is RunStatus.Cancelled or RunStatus.WaitingForHuman or RunStatus.HumanResponding)
        {
            return false;
        }

        store.AddUserMessage(runId, run.TravellerName, request.Message);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);

        _ = Task.Run(() => ProcessTurnSafelyAsync(runId, request.Message, CancellationToken.None), CancellationToken.None);
        return true;
    }

    public async Task<bool> CancelAsync(Guid runId, CancellationToken cancellationToken)
    {
        if (!store.TryGet(runId, out _))
        {
            return false;
        }

        store.SetStatus(runId, RunStatus.Cancelled, "Run cancelled.");
        await humanSupport.CancelAsync(runId).ConfigureAwait(false);
        await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task ProcessTurnSafelyAsync(Guid runId, string message, CancellationToken cancellationToken)
    {
        SemaphoreSlim turnLock = _locks.GetOrAdd(runId, _ => new SemaphoreSlim(1, 1));
        await turnLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!store.TryGet(runId, out TravelRun run) || run.Status == RunStatus.Cancelled)
            {
                return;
            }

            RunEventWriter writer = new(store, notifier, runId);
            await foundryRunner.ProcessTurnAsync(run, message, writer, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Run {RunId} failed.", runId);
            store.AddSystemMessage(runId, ex.Message);
            store.SetStatus(runId, RunStatus.Failed, "Run failed.");
            await notifier.NotifyRunAsync(runId, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            turnLock.Release();
        }
    }
}
