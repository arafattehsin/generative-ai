using System.Collections.Concurrent;
using TravelConcierge.Api.Models;

namespace TravelConcierge.Api.Services;

public sealed class RunStore
{
    private readonly ConcurrentDictionary<Guid, TravelRun> _runs = new();

    public TravelRun Create(CreateRunRequest request)
    {
        TravelRun run = new()
        {
            Mode = "foundry",
            TravellerName = request.TravellerName,
            TripCode = request.TripCode,
            Urgency = request.Urgency,
            InitialRequest = request.Message,
            Summary = "Live Agent Framework handoff run queued.",
        };

        run.Messages.Add(new ConversationMessage
        {
            AuthorType = ConversationAuthor.User,
            AuthorName = request.TravellerName,
            Text = request.Message,
        });

        _runs[run.Id] = run;
        return run;
    }

    public IReadOnlyList<TravelRun> GetAll() =>
        _runs.Values
            .OrderByDescending(run => run.CreatedAt)
            .ToList();

    public TravelRun? Get(Guid id) =>
        _runs.TryGetValue(id, out TravelRun? run) ? run : null;

    public bool TryGet(Guid id, out TravelRun run) =>
        _runs.TryGetValue(id, out run!);

    public void AddUserMessage(Guid runId, string authorName, string text)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            run.Messages.Add(new ConversationMessage
            {
                AuthorType = ConversationAuthor.User,
                AuthorName = authorName,
                Text = text,
            });
            Touch(run);
        }
    }

    public Guid AddAgentMessage(Guid runId, AgentDefinition agent, string text = "")
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return Guid.Empty;
        }

        ConversationMessage message = new()
        {
            AuthorType = ConversationAuthor.Agent,
            AgentId = agent.Id,
            AuthorName = agent.Name,
            Text = text,
        };

        lock (run.SyncRoot)
        {
            run.Messages.Add(message);
            Touch(run);
        }

        return message.Id;
    }

    public Guid AddHumanMessage(Guid runId, string operatorName, string text)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return Guid.Empty;
        }

        ConversationMessage message = new()
        {
            AuthorType = ConversationAuthor.Human,
            AgentId = TravelAgents.Human.Id,
            AuthorName = operatorName,
            Text = text,
        };

        lock (run.SyncRoot)
        {
            run.Messages.Add(message);
            Touch(run);
        }

        return message.Id;
    }

    public void AppendAgentMessage(Guid runId, Guid messageId, string text)
    {
        if (!TryGet(runId, out TravelRun run) || string.IsNullOrEmpty(text))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            ConversationMessage? message = run.Messages.FirstOrDefault(item => item.Id == messageId);
            if (message != null)
            {
                message.Text += text;
                Touch(run);
            }
        }
    }

    public void AddSystemMessage(Guid runId, string text)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            run.Messages.Add(new ConversationMessage
            {
                AuthorType = ConversationAuthor.System,
                AuthorName = "System",
                Text = text,
            });
            Touch(run);
        }
    }

    public void SetStatus(Guid runId, RunStatus status, string? summary = null)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            run.Status = status;
            if (!string.IsNullOrWhiteSpace(summary))
            {
                run.Summary = summary;
            }
            Touch(run);
        }
    }

    public void SetOwner(Guid runId, AgentDefinition agent)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            run.CurrentOwnerId = agent.Id;
            run.CurrentOwnerName = agent.Name;
            Touch(run);
        }
    }

    public void RequestHumanSupport(Guid runId, string requestId, HumanSupportRequest request)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            run.HumanSupport = new HumanSupportSnapshot
            {
                RequestId = requestId,
                RequestedByAgentId = request.RequestedByAgentId,
                RequestedByAgentName = request.RequestedByAgentName,
                Reason = request.Reason,
                DecisionNeeded = request.DecisionNeeded,
                Recommendation = request.Recommendation,
                ConversationSummary = request.ConversationSummary,
            };
            run.Status = RunStatus.WaitingForHuman;
            run.Summary = "A recovery specialist is reviewing the case.";
            run.CurrentOwnerId = TravelAgents.Human.Id;
            run.CurrentOwnerName = TravelAgents.Human.Name;
            Touch(run);
        }
    }

    public bool ClaimHumanSupport(Guid runId, string operatorName)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return false;
        }

        lock (run.SyncRoot)
        {
            if (run.HumanSupport is null || run.HumanSupport.Status == HumanSupportStatus.Resolved)
            {
                return false;
            }

            if (run.HumanSupport.Status == HumanSupportStatus.Claimed &&
                !string.Equals(run.HumanSupport.AssignedTo, operatorName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            run.HumanSupport.Status = HumanSupportStatus.Claimed;
            run.HumanSupport.AssignedTo = operatorName;
            run.HumanSupport.ClaimedAt ??= DateTimeOffset.UtcNow;
            run.Status = RunStatus.HumanResponding;
            run.Summary = $"{operatorName} is reviewing the recovery decision.";
            run.CurrentOwnerId = TravelAgents.Human.Id;
            run.CurrentOwnerName = operatorName;
            Touch(run);
            return true;
        }
    }

    public void ResolveHumanSupport(Guid runId, HumanSupportResolution resolution)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            if (run.HumanSupport is null)
            {
                return;
            }

            run.HumanSupport.Status = HumanSupportStatus.Resolved;
            run.HumanSupport.AssignedTo = resolution.OperatorName;
            run.HumanSupport.ResolvedAt = DateTimeOffset.UtcNow;
            Touch(run);
        }
    }

    public void AddHandoff(Guid runId, AgentDefinition from, AgentDefinition to, string reason)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            run.Timeline.Add(new HandoffTimelineItem(
                Guid.NewGuid(),
                from.Id,
                from.Name,
                to.Id,
                to.Name,
                reason,
                DateTimeOffset.UtcNow));

            run.CurrentOwnerId = to.Id;
            run.CurrentOwnerName = to.Name;
            Touch(run);
        }
    }

    public void UpdateCase(Guid runId, TravelCaseSnapshot snapshot)
    {
        if (!TryGet(runId, out TravelRun run))
        {
            return;
        }

        lock (run.SyncRoot)
        {
            run.Case = snapshot;
            Touch(run);
        }
    }

    private static void Touch(TravelRun run) => run.UpdatedAt = DateTimeOffset.UtcNow;
}
