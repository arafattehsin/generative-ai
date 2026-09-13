using System.Collections.Concurrent;
using Azure.AI.Projects;
using Azure.Identity;
using CivicWorks.Api.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.AI;

namespace CivicWorks.Api.Services;

public sealed class LiveMagenticRunService(
    FoundryOptions foundryOptions,
    CivicWorksRunStore store,
    RunNotifier notifier,
    ILogger<LiveMagenticRunService> logger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, LiveSession> _sessions = new();

    private const string TaskPrompt = """
        Prepare a Preliminary Works Options Brief for the fictional NSW local-council case CW-2047, Marrin Precinct.

        OBJECTIVE
        Investigate recurring ponding, an incomplete accessible route between Marrin Library, the community centre and bus stop 214, civil asset condition, place constraints, indicative cost and delivery impacts. Produce options A, B and C and recommend only the best-supported next route.

        INITIAL-PLAN BOUNDARY
        The initial plan must test the known route in this order without inventing facts: service requests and access impacts; accessible-route evidence; civil asset register; current site observation; place constraints; then preliminary options. Do not add heritage verification, a non-invasive survey or an excavation-avoiding option until collected evidence demonstrates that those checks are necessary.

        EVIDENCE-LED ADAPTATION
        New tool-backed evidence may invalidate the initial route. Do not speculate about what that evidence might contain and do not revise the plan from scenario wording alone. Compare actual specialist findings and their evidence references. If collected evidence invalidates the current route, rebuild it around the checks that the evidence itself requires.

        EVIDENCE AND COMPLETION RULES
        Use only the read-only tools attached to the specialists. Every factual claim must retain evidence references. Do not treat absence from a register as approval or as a heritage determination. The request is not satisfied until EV-REPORT-01 exists and all options cite evidence. If the budget prevents that, stop with an honest bounded-incomplete result.

        AUTHORITY LIMITS
        This run is decision support only. It cannot approve engineering, heritage, expenditure or works; contact residents, businesses or contractors; create a work order; or present fictional ranges as real council estimates.

        FINAL OUTPUT CONTRACT
        Return exactly one JSON object and no prose outside it. Use this shape:
        {
          "recommendation": "A|B|C",
          "summary": "short decision-support summary",
          "options": [
            { "code": "A", "title": "...", "assessment": "...", "supportStatus": "..." },
            { "code": "B", "title": "...", "assessment": "...", "supportStatus": "..." },
            { "code": "C", "title": "...", "assessment": "...", "supportStatus": "..." }
          ],
          "claims": [
            { "claim": "...", "evidenceReferences": ["..."] }
          ],
          "openMatters": ["..."]
        }
        """;

    private const string ManagerInstructions = """
        You are the CivicWorks Magentic Manager. Your only role is to plan, select the next named specialist, track whether the evidence-bound task is progressing, trigger a genuine replan when the current route is invalidated, and prepare the final JSON. Do not perform specialist work and do not invent council evidence.

        Use exact participant names from the team. Keep the initial plan inside the INITIAL-PLAN BOUNDARY in the task.

        A PLAN_INVALIDATED signal is valid only when the manager-visible conversation contains tool-backed specialist findings for both AR-DN-44 and OBS-07, and the OBS-07 response says the signal came from get_site_observation. If either reference is absent, do not replan and do not infer the conflict: select the missing specialist and require its read-only tool. Once both references exist and the current plan is still the original route, set the next progress ledger to is_in_loop=true and is_progress_being_made=false so the framework resets it.

        The rebuilt plan must add heritage-register verification, a non-invasive site survey, an excavation-avoiding option, cost and disruption recalculation, and an Evidence Verifier challenge. Once those revised steps are present, treat the earlier signal as addressed: set progress according to the new work and never trigger another reset from historical conflict text. Never declare the request satisfied before EV-REPORT-01 exists. The final answer must obey the FINAL OUTPUT CONTRACT exactly, with evidence references on every claim.
        """;

    public CivicWorksRunSnapshot StartRun()
    {
        foundryOptions.Validate();
        CivicWorksRunSnapshot snapshot = store.Create();
        _ = ExecuteAsync(snapshot.Id);
        return snapshot;
    }

    public async Task<CivicWorksRunSnapshot> ReviewPlanAsync(
        Guid runId,
        PlanReviewCommand command,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(runId, out LiveSession? session))
        {
            throw new InvalidOperationException("This run does not have a live plan-review request.");
        }

        await session.ReviewLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ExternalRequest request;
            MagenticPlanReviewRequest review;

            lock (session.SyncRoot)
            {
                request = session.PendingRequest
                    ?? throw new InvalidOperationException("The live workflow is not currently awaiting plan review.");
                review = session.PendingReview
                    ?? throw new InvalidOperationException("The live plan-review payload is unavailable.");
                session.PendingRequest = null;
                session.PendingReview = null;
            }

            MagenticPlanReviewResponse response = command.Action.Trim().ToLowerInvariant() switch
            {
                "approve" => review.Approve(),
                "revise" when !string.IsNullOrWhiteSpace(command.Feedback) => review.Revise(command.Feedback.Trim()),
                "revise" => throw new ArgumentException("Revision feedback is required when action is 'revise'."),
                _ => throw new ArgumentException("Plan-review action must be 'approve' or 'revise'."),
            };

            string constraints = command.Constraints is { Count: > 0 }
                ? $" Confirmed constraints: {string.Join("; ", command.Constraints)}."
                : string.Empty;

            CivicWorksRunSnapshot snapshot = store.Update(runId, state =>
            {
                if (command.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
                {
                    int planIndex = state.Plans.FindIndex(plan => plan.Version == state.PlanVersion);
                    if (planIndex >= 0)
                    {
                        state.Plans[planIndex] = state.Plans[planIndex] with
                        {
                            ApprovedAt = DateTimeOffset.UtcNow,
                            Constraints = command.Constraints?.ToArray() ?? [],
                        };
                    }
                }
                state.Phase = RunPhase.Running;
                state.StatusMessage = command.Action.Equals("revise", StringComparison.OrdinalIgnoreCase)
                    ? "The officer requested a live plan revision."
                    : $"The officer approved Plan {Math.Max(1, state.PlanVersion):00}.{constraints}";
                state.ActiveSpecialist = "Magentic Manager";
                state.ActiveActivity = "Resuming the live workflow";
            });
            await notifier.PublishAsync(runId, cancellationToken).ConfigureAwait(false);

            await session.WorkflowRun
                .SendResponseAsync(request.CreateResponse(response))
                .ConfigureAwait(false);

            return snapshot;
        }
        finally
        {
            session.ReviewLock.Release();
        }
    }

    private async Task ExecuteAsync(Guid runId)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(CivicWorksRunState.MaximumElapsedSeconds));
        CancellationToken cancellationToken = timeout.Token;

        try
        {
            store.Update(runId, state =>
            {
                state.Phase = RunPhase.Planning;
                state.StatusMessage = "The live Magentic manager is creating Plan 01.";
                state.ActiveSpecialist = "Magentic Manager";
                state.ActiveActivity = "Creating the initial task ledger";
            });
            await notifier.PublishAsync(runId, cancellationToken).ConfigureAwait(false);

            AIProjectClient projectClient = new(
                new Uri(foundryOptions.ProjectEndpoint!),
                new AzureCliCredential());

            CouncilEvidenceTools evidenceTools = new(runId, store, notifier);
            IReadOnlyList<AIAgent> participants = CreateParticipants(projectClient, evidenceTools);
            AIAgent manager = projectClient.AsAIAgent(new ChatClientAgentOptions
            {
                Name = "MagenticManager",
                Description = "Plans, tracks, and genuinely replans the bounded CivicWorks investigation.",
                ChatOptions = new ChatOptions
                {
                    ModelId = foundryOptions.ModelDeployment!,
                    Instructions = ManagerInstructions,
                },
                AIContextProviders = [new EvidenceSnapshotContextProvider(runId, store)],
            });

            Workflow workflow = new MagenticWorkflowBuilder(manager)
                .AddParticipants(participants)
                .WithName("CivicWorks Magentic Investigation")
                .WithDescription("Produces an evidence-bound preliminary works options brief for fictional case CW-2047.")
                .RequirePlanSignoff(true)
                .WithMaxRounds(CivicWorksRunState.MaximumRounds)
                .WithMaxStalls(CivicWorksRunState.MaximumStallsBeforeReplan)
                // The framework checks the reset limit before the next round, so 2 permits one completed replan.
                .WithMaxResets(CivicWorksRunState.MaximumResets + 1)
                .Build();

            await using StreamingRun streamingRun = await InProcessExecution.RunStreamingAsync(
                workflow,
                new List<ChatMessage> { new(ChatRole.User, TaskPrompt) },
                CheckpointManager.CreateInMemory(),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            LiveSession session = new(streamingRun, timeout);
            if (!_sessions.TryAdd(runId, session))
            {
                throw new InvalidOperationException("A live workflow session already exists for this run.");
            }

            await streamingRun.TrySendMessageAsync(new TurnToken(emitEvents: true)).ConfigureAwait(false);

            bool completed = false;
            await foreach (WorkflowEvent workflowEvent in streamingRun.WatchStreamAsync().WithCancellation(cancellationToken))
            {
                switch (workflowEvent)
                {
                    case MagenticPlanCreatedEvent planCreated:
                        await RecordPlanAsync(runId, planCreated.FullTaskLedger.Text, isReplan: false, cancellationToken).ConfigureAwait(false);
                        break;

                    case MagenticReplannedEvent replanned:
                        await RecordPlanAsync(runId, replanned.FullTaskLedger.Text, isReplan: true, cancellationToken).ConfigureAwait(false);
                        break;

                    case MagenticProgressLedgerUpdatedEvent progress:
                        await RecordProgressAsync(runId, progress.ProgressLedger, cancellationToken).ConfigureAwait(false);
                        break;

                    case AgentResponseUpdateEvent update:
                        await RecordAgentActivityAsync(runId, update.ExecutorId, cancellationToken).ConfigureAwait(false);
                        break;

                    case RequestInfoEvent requestEvent
                        when requestEvent.Request.TryGetDataAs<MagenticPlanReviewRequest>(out MagenticPlanReviewRequest? review)
                             && review is not null:
                        lock (session.SyncRoot)
                        {
                            session.PendingRequest = requestEvent.Request;
                            session.PendingReview = review;
                        }
                        await RecordPlanReviewAsync(runId, review, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkflowOutputEvent outputEvent when outputEvent.Is<List<ChatMessage>>():
                        List<ChatMessage> outputMessages = outputEvent.As<List<ChatMessage>>()
                            ?? throw new InvalidOperationException("The live Magentic workflow returned an empty output payload.");
                        await CompleteAsync(runId, outputMessages, cancellationToken).ConfigureAwait(false);
                        completed = true;
                        break;

                    case WorkflowErrorEvent workflowError:
                        throw workflowError.Exception ?? new InvalidOperationException("The live Magentic workflow failed.");

                    case ExecutorFailedEvent executorFailed:
                        throw new InvalidOperationException(
                            $"Live workflow executor '{executorFailed.ExecutorId}' failed: {executorFailed.Data}");
                }

                if (completed)
                {
                    break;
                }
            }

            if (!completed)
            {
                throw new InvalidOperationException("The live Magentic event stream ended without a final output.");
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            await MarkBoundedIncompleteAsync(
                runId,
                "The live investigation reached its elapsed-time budget. No fallback brief was generated.").ConfigureAwait(false);
        }
        catch (AuthenticationFailedException ex)
        {
            await MarkFailedAsync(
                runId,
                "Azure CLI authentication could not access Microsoft Foundry. Run 'az login --scope https://ai.azure.com/.default' and try again.",
                ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(runId, ex.Message, ex).ConfigureAwait(false);
        }
        finally
        {
            _sessions.TryRemove(runId, out _);
        }
    }

    private IReadOnlyList<AIAgent> CreateParticipants(AIProjectClient projectClient, CouncilEvidenceTools tools)
    {
        string model = foundryOptions.ModelDeployment!;

        AIAgent communityAccess = projectClient.AsAIAgent(
            model,
            name: "CommunityAccessAnalyst",
            description: "Verifies fictional service-request patterns and accessible-route impacts using read-only evidence tools.",
            instructions: "You are the Community & Access Analyst. Use both attached tools for CW-2047. Report concise findings with exact evidence references. Do not invent residents, impacts, or approvals.",
            tools:
            [
                AIFunctionFactory.Create(tools.GetServiceRequestsAsync, "get_service_requests", "Read the synthetic, deidentified service-request register for the fixed CivicWorks case CW-2047. This tool takes no arguments."),
                AIFunctionFactory.Create(tools.GetAccessAuditAsync, "get_access_audit", "Read the synthetic accessible-route audit for the fixed CivicWorks case CW-2047. This tool takes no arguments."),
            ]);

        AIAgent civilAssets = projectClient.AsAIAgent(
            model,
            name: "CivilAssetsAnalyst",
            description: "Checks the fictional drainage asset register and exposes alignment uncertainty.",
            instructions: "You are the Civil Assets Analyst. Use the attached read-only tool for CW-2047. State what the register says, its reliability limit, and its evidence reference. Never treat a legacy map as field verification.",
            tools:
            [
                AIFunctionFactory.Create(tools.GetCivilAssetRecordAsync, "get_civil_asset_record", "Read the synthetic civil drainage asset record for the fixed CivicWorks case CW-2047. This tool takes no arguments."),
            ]);

        AIAgent placeConstraints = projectClient.AsAIAgent(
            model,
            name: "PlaceConstraintsAdvisor",
            description: "Compares the current site observation with place constraints and performs added checks after a genuine replan.",
            instructions: "You are the Place & Constraints Advisor. Follow the current manager plan exactly. Before a replan, you must call get_site_observation and get_place_constraints; do not answer from the task description. Repeat PLAN_INVALIDATED only when get_site_observation returned it in the same response, and cite OBS-07. After the revised plan is approved, use the heritage-register and non-invasive-survey tools. Never claim heritage significance or permission to disturb.",
            tools:
            [
                AIFunctionFactory.Create(tools.GetSiteObservationAsync, "get_site_observation", "Read the synthetic current site observation for the fixed CivicWorks case CW-2047. This tool takes no arguments."),
                AIFunctionFactory.Create(tools.GetTreeAndAccessConstraintsAsync, "get_place_constraints", "Read the synthetic tree, business-access, and accessible-route delivery constraints."),
                AIFunctionFactory.Create(tools.GetHeritageRegisterCheckAsync, "get_heritage_register_check", "Read the synthetic heritage-register check after the revised plan is approved."),
                AIFunctionFactory.Create(tools.GetNonInvasiveSurveyBriefAsync, "get_non_invasive_survey_brief", "Read the synthetic non-invasive survey method after the revised plan is approved."),
            ]);

        AIAgent costDelivery = projectClient.AsAIAgent(
            model,
            name: "CostDeliveryAnalyst",
            description: "Compares three fictional indicative options only after the revised evidence prerequisites exist.",
            instructions: "You are the Cost & Delivery Analyst. Use the attached tool for CW-2047. Treat all ranges as fictional and indicative. If the tool reports blocked or PLAN_INVALIDATED, do not estimate or improvise; report the missing evidence to the manager.",
            tools:
            [
                AIFunctionFactory.Create(tools.EstimateWorksOptionsAsync, "estimate_works_options", "Compare the three synthetic options after the required revised-plan evidence exists."),
            ]);

        AIAgent verifier = projectClient.AsAIAgent(
            model,
            name: "EvidenceVerifier",
            description: "Challenges the complete evidence packet and refuses unsupported recommendations.",
            instructions: "You are the Evidence Verifier. Use the attached deterministic integrity tool for CW-2047. If it reports incomplete, identify the missing evidence and say the request is not satisfied. If complete, retain the exact evidence references, unresolved matters, and decision-support boundary.",
            tools:
            [
                AIFunctionFactory.Create(tools.VerifyEvidencePacketAsync, "verify_evidence_packet", "Check whether every required synthetic evidence item exists and every recommendation condition is supportable."),
            ]);

        return [communityAccess, civilAssets, placeConstraints, costDelivery, verifier];
    }

    private async Task RecordPlanAsync(Guid runId, string? planText, bool isReplan, CancellationToken cancellationToken)
    {
        store.Update(runId, state =>
        {
            state.PlanVersion = isReplan ? Math.Max(2, state.PlanVersion + 1) : 1;
            state.PlanText = planText;
            state.Phase = RunPhase.Planning;
            state.ActiveSpecialist = "Magentic Manager";
            state.ActiveActivity = isReplan ? "Rebuilding the task ledger" : "Proposed the initial task ledger";
            state.StatusMessage = isReplan
                ? "The live manager rebuilt the route after the evidence conflict."
                : "Plan 01 was generated by the live Magentic manager.";

            if (isReplan)
            {
                state.ResetsUsed++;
                state.RevisionReason = "OBS-07 conflicts with the mapped drainage asset AR-DN-44.";
                state.FocusEvidenceId = "sandstone";
            }
        });
        await notifier.PublishAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    private async Task RecordProgressAsync(Guid runId, MagenticProgressLedger progress, CancellationToken cancellationToken)
    {
        store.Update(runId, state =>
        {
            state.RoundsUsed++;
            if (progress.IsInLoop || !progress.IsProgressBeingMade)
            {
                state.StallsObserved++;
            }

            state.ActiveSpecialist = DisplayName(progress.NextSpeaker);
            state.ActiveActivity = progress.InstructionOrQuestion;
            state.StatusMessage = progress.IsRequestSatisfied
                ? "The manager is preparing the evidence-bound final answer."
                : $"Round {state.RoundsUsed}: {DisplayName(progress.NextSpeaker)} selected by the live manager.";
        });
        await notifier.PublishAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    private async Task RecordAgentActivityAsync(Guid runId, string executorId, CancellationToken cancellationToken)
    {
        string displayName = DisplayName(executorId);
        CivicWorksRunSnapshot snapshot = store.Update(runId, state =>
        {
            if (!string.Equals(state.ActiveSpecialist, displayName, StringComparison.Ordinal))
            {
                state.ActiveSpecialist = displayName;
                state.ActiveActivity = "Responding through Microsoft Foundry";
            }
        });

        await notifier.PublishAsync(snapshot.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task RecordPlanReviewAsync(Guid runId, MagenticPlanReviewRequest review, CancellationToken cancellationToken)
    {
        store.Update(runId, state =>
        {
            bool revised = review.CurrentProgress is not null || state.PlanVersion > 1;
            state.Phase = revised ? RunPhase.RevisedReview : RunPhase.InitialReview;
            state.PlanText = review.Plan.Text;
            state.ActiveSpecialist = null;
            state.ActiveActivity = null;
            state.StatusMessage = revised
                ? $"The live workflow is paused for officer review of Plan {Math.Max(2, state.PlanVersion):00}."
                : "The live workflow is paused for officer review of Plan 01.";
        });
        await notifier.PublishAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    private async Task CompleteAsync(Guid runId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        string finalText = messages.LastOrDefault()?.Text ?? string.Empty;
        if (finalText.Contains("maximum", StringComparison.OrdinalIgnoreCase) &&
            finalText.Contains("limit", StringComparison.OrdinalIgnoreCase))
        {
            await MarkBoundedIncompleteAsync(runId, finalText).ConfigureAwait(false);
            return;
        }

        PreliminaryWorksOptionsBrief brief = PreliminaryWorksOptionsBriefParser.Parse(finalText);
        store.Update(runId, state =>
        {
            state.Brief = brief;
            state.Phase = RunPhase.Completed;
            state.StatusMessage = "The live Magentic investigation produced a reviewable preliminary options brief.";
            state.ActiveSpecialist = "Magentic Manager";
            state.ActiveActivity = "Final evidence-bound brief complete";
            state.FocusEvidenceId = "verification";
        });
        await notifier.PublishAsync(runId, cancellationToken).ConfigureAwait(false);
    }

    private async Task MarkBoundedIncompleteAsync(Guid runId, string message)
    {
        store.Update(runId, state =>
        {
            state.Phase = RunPhase.BoundedIncomplete;
            state.StatusMessage = message;
            state.Error = message;
            state.ActiveSpecialist = null;
            state.ActiveActivity = null;
        });
        await notifier.PublishAsync(runId).ConfigureAwait(false);
    }

    private async Task MarkFailedAsync(Guid runId, string message, Exception exception)
    {
        logger.LogError(exception, "CivicWorks live run {RunId} failed.", runId);
        store.Update(runId, state =>
        {
            state.Phase = RunPhase.Failed;
            state.StatusMessage = message;
            state.Error = message;
            state.ActiveSpecialist = null;
            state.ActiveActivity = null;
        });
        await notifier.PublishAsync(runId).ConfigureAwait(false);
    }

    private static string DisplayName(string? agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            return "Magentic Manager";
        }

        return agentName switch
        {
            "CommunityAccessAnalyst" => "Community & Access Analyst",
            "CivilAssetsAnalyst" => "Civil Assets Analyst",
            "PlaceConstraintsAdvisor" => "Place & Constraints Advisor",
            "CostDeliveryAnalyst" => "Cost & Delivery Analyst",
            "EvidenceVerifier" => "Evidence Verifier",
            "MagenticManager" => "Magentic Manager",
            _ when agentName.Contains("CommunityAccessAnalyst", StringComparison.Ordinal) => "Community & Access Analyst",
            _ when agentName.Contains("CivilAssetsAnalyst", StringComparison.Ordinal) => "Civil Assets Analyst",
            _ when agentName.Contains("PlaceConstraintsAdvisor", StringComparison.Ordinal) => "Place & Constraints Advisor",
            _ when agentName.Contains("CostDeliveryAnalyst", StringComparison.Ordinal) => "Cost & Delivery Analyst",
            _ when agentName.Contains("EvidenceVerifier", StringComparison.Ordinal) => "Evidence Verifier",
            _ => agentName,
        };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (LiveSession session in _sessions.Values)
        {
            session.Timeout.Cancel();
            await session.WorkflowRun.DisposeAsync().ConfigureAwait(false);
        }
        _sessions.Clear();
    }

    private sealed class LiveSession(StreamingRun workflowRun, CancellationTokenSource timeout)
    {
        public object SyncRoot { get; } = new();
        public SemaphoreSlim ReviewLock { get; } = new(1, 1);
        public StreamingRun WorkflowRun { get; } = workflowRun;
        public CancellationTokenSource Timeout { get; } = timeout;
        public ExternalRequest? PendingRequest { get; set; }
        public MagenticPlanReviewRequest? PendingReview { get; set; }
    }
}
