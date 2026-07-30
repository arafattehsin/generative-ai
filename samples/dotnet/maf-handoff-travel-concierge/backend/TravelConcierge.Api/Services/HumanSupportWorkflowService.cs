using System.Collections.Concurrent;
using Microsoft.Agents.AI.Workflows;
using TravelConcierge.Api.Models;

namespace TravelConcierge.Api.Services;

public sealed class HumanSupportWorkflowService(
    RunStore store,
    RunEventNotifier notifier,
    ILogger<HumanSupportWorkflowService> logger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, PendingHumanSession> _pending = new();

    public IReadOnlyList<TravelRun> GetQueue() =>
        store.GetAll()
            .Where(run => run.HumanSupport is not null && run.HumanSupport.Status != HumanSupportStatus.Resolved)
            .OrderByDescending(run => string.Equals(run.Urgency, "Critical", StringComparison.OrdinalIgnoreCase))
            .ThenBy(run => run.HumanSupport!.RequestedAt)
            .ToList();

    public async Task StartAsync(
        TravelRun run,
        AgentDefinition requestedBy,
        string escalationNote,
        RunEventWriter writer,
        CancellationToken cancellationToken)
    {
        if (_pending.ContainsKey(run.Id))
        {
            return;
        }

        HumanSupportRequest request = BuildRequest(run, requestedBy, escalationNote);
        RequestPort humanSupportPort = RequestPort.Create<HumanSupportRequest, HumanSupportResolution>("HumanSupport");
        HumanSupportCompletionExecutor completion = new();
        Workflow workflow = new WorkflowBuilder(humanSupportPort)
            .AddEdge(humanSupportPort, completion)
            .WithOutputFrom(completion)
            .Build();

        StreamingRun workflowRun = await InProcessExecution
            .RunStreamingAsync(workflow, request, CheckpointManager.Default, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        PendingHumanSession pending = new(run.Id, workflowRun, writer);
        if (!_pending.TryAdd(run.Id, pending))
        {
            await workflowRun.DisposeAsync().ConfigureAwait(false);
            return;
        }

        pending.Observer = ObserveAsync(pending);
        await pending.Ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ClaimAsync(Guid runId, string operatorName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operatorName) || !_pending.ContainsKey(runId))
        {
            return false;
        }

        bool claimed = store.ClaimHumanSupport(runId, operatorName.Trim());
        if (claimed)
        {
            await notifier.NotifyRunAsync(runId, cancellationToken).ConfigureAwait(false);
        }

        return claimed;
    }

    public async Task<bool> ResolveAsync(Guid runId, HumanSupportResolution resolution, CancellationToken cancellationToken)
    {
        if (!_pending.TryGetValue(runId, out PendingHumanSession? pending) || pending.Request is null)
        {
            return false;
        }

        if (!store.TryGet(runId, out TravelRun run) ||
            run.HumanSupport?.Status != HumanSupportStatus.Claimed ||
            !string.Equals(run.HumanSupport.AssignedTo, resolution.OperatorName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        await pending.WorkflowRun
            .SendResponseAsync(pending.Request.CreateResponse(resolution))
            .ConfigureAwait(false);
        await pending.Completed.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task CancelAsync(Guid runId)
    {
        if (_pending.TryRemove(runId, out PendingHumanSession? pending))
        {
            pending.Completed.TrySetCanceled();
            await pending.WorkflowRun.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach ((Guid runId, PendingHumanSession pending) in _pending.ToArray())
        {
            if (_pending.TryRemove(runId, out _))
            {
                await pending.WorkflowRun.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task ObserveAsync(PendingHumanSession pending)
    {
        try
        {
            await foreach (WorkflowEvent evt in pending.WorkflowRun.WatchStreamAsync().ConfigureAwait(false))
            {
                switch (evt)
                {
                    case RequestInfoEvent requestEvent when requestEvent.Request.TryGetDataAs<HumanSupportRequest>(out HumanSupportRequest? request):
                        pending.Request = requestEvent.Request;
                        await pending.Writer.RequestHumanSupportAsync(requestEvent.Request.RequestId, request!).ConfigureAwait(false);
                        pending.Ready.TrySetResult();
                        break;

                    case WorkflowOutputEvent output when output.Data is HumanSupportResolution resolution:
                        await CompleteAsync(pending, resolution).ConfigureAwait(false);
                        return;

                    case WorkflowErrorEvent error:
                        throw error.Exception ?? new InvalidOperationException("Human support workflow failed.");

                    case ExecutorFailedEvent failed:
                        throw new InvalidOperationException($"Human support executor '{failed.ExecutorId}' failed: {failed.Data}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Human support workflow failed for run {RunId}.", pending.RunId);
            pending.Ready.TrySetException(ex);
            pending.Completed.TrySetException(ex);
            await pending.Writer.AddSystemMessageAsync("We could not connect the recovery specialist. Please try again.").ConfigureAwait(false);
            await pending.Writer.SetStatusAsync(Models.RunStatus.Failed, "Human support could not be connected.").ConfigureAwait(false);
        }
        finally
        {
            if (_pending.TryRemove(pending.RunId, out _))
            {
                await pending.WorkflowRun.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static HumanSupportRequest BuildRequest(TravelRun run, AgentDefinition requestedBy, string escalationNote)
    {
        string summary = string.Join(Environment.NewLine, run.Messages.TakeLast(6).Select(message =>
            $"{message.AuthorName}: {message.Text}"));

        return new HumanSupportRequest(
            run.Id,
            run.TravellerName,
            run.TripCode,
            run.Urgency,
            requestedBy.Id,
            requestedBy.Name,
            "The traveller has a time-critical recovery need that requires a person to review the exception.",
            "Confirm or adjust the fastest simulated recovery path, then explain the decision to the traveller.",
            string.IsNullOrWhiteSpace(escalationNote)
                ? "Prioritise a confirmed arrival before the medical appointment; if that is impossible, protect the traveller overnight."
                : escalationNote.Trim(),
            summary);
    }

    private async Task CompleteAsync(PendingHumanSession pending, HumanSupportResolution resolution)
    {
        AgentDefinition nextOwner = TravelAgents.Resolve(resolution.NextOwnerId);
        if (nextOwner.Id == TravelAgents.Human.Id)
        {
            nextOwner = TravelAgents.Insurance;
        }

        AgentDefinition humanOwner = TravelAgents.Human with { Name = resolution.OperatorName };
        await pending.Writer.ResolveHumanSupportAsync(resolution).ConfigureAwait(false);
        await pending.Writer.AddHumanMessageAsync(resolution.OperatorName, resolution.Message).ConfigureAwait(false);
        await pending.Writer.HandoffAsync(humanOwner, nextOwner,
            $"{resolution.OperatorName} completed the manual review and returned ownership to {nextOwner.Name}.").ConfigureAwait(false);
        await pending.Writer.SetStatusAsync(Models.RunStatus.WaitingForUser,
            $"{nextOwner.Name} is ready for the traveller's next message.").ConfigureAwait(false);
        pending.Completed.TrySetResult();
    }

    private sealed class PendingHumanSession(Guid runId, StreamingRun workflowRun, RunEventWriter writer)
    {
        public Guid RunId { get; } = runId;
        public StreamingRun WorkflowRun { get; } = workflowRun;
        public RunEventWriter Writer { get; } = writer;
        public ExternalRequest? Request { get; set; }
        public Task? Observer { get; set; }
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

internal sealed class HumanSupportCompletionExecutor()
    : Executor<HumanSupportResolution, HumanSupportResolution>("HumanSupportCompletion")
{
    public override ValueTask<HumanSupportResolution> HandleAsync(
        HumanSupportResolution message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(message);
}
