// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OnboardFlow.Application.Executors;
using OnboardFlow.Application.Executors.Maf;
using OnboardFlow.Application.Interfaces;
using OnboardFlow.Application.Utilities;
using OnboardFlow.Domain.Entities;
using OnboardFlow.Domain.Enums;
using OnboardFlow.Domain.ValueObjects;

namespace OnboardFlow.Application.Orchestration;

/// <summary>
/// Orchestrates the OnboardFlow pipeline:
///   Sequential: IntakeNormalize → ExtractProfile
///   Concurrent: SecurityReview + ComplianceReview + FinanceReview → AggregateFindings (MAF fan-out/fan-in)
///   Sequential: CustomerNextSteps → FinalPackage
/// </summary>
public sealed class OnboardFlowOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWorkflowProgressHub _progressHub;
    private readonly IChatClient _chatClient;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningWorkflows = new();

    public OnboardFlowOrchestrator(
        IServiceScopeFactory scopeFactory,
        IWorkflowProgressHub progressHub,
        IChatClient chatClient)
    {
        _scopeFactory = scopeFactory;
        _progressHub = progressHub;
        _chatClient = chatClient;
    }

    public async Task<WorkflowRun> StartRunAsync(string inputText, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();
        var stepRepo = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();
        var piiService = scope.ServiceProvider.GetRequiredService<IPiiRedactionService>();

        WorkflowRun run = new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = WorkflowStatus.Pending,
            InputTextOriginal = inputText
        };

        RedactionResult redactionResult = piiService.Redact(inputText);
        run.InputTextRedacted = redactionResult.RedactedText;

        await runRepo.CreateAsync(run, cancellationToken);

        foreach (string stepName in WorkflowSteps.AllSteps)
        {
            WorkflowStepRun step = new()
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                StepName = stepName,
                StepOrder = WorkflowSteps.GetStepOrder(stepName),
                Status = StepStatus.Pending,
                ConcurrencyGroup = WorkflowSteps.IsConcurrentStep(stepName)
                    ? WorkflowSteps.ConcurrencyGroupName
                    : null
            };
            await stepRepo.CreateAsync(step, cancellationToken);
        }

        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningWorkflows[run.Id] = linkedCts;

        _ = ExecuteWorkflowAsync(run.Id, null, linkedCts.Token);

        return run;
    }

    public async Task<WorkflowRun> RerunFromStepAsync(Guid parentRunId, string fromStep, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();
        var stepRepo = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();

        WorkflowRun? parentRun = await runRepo.GetByIdAsync(parentRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Parent run {parentRunId} not found.");

        Guid rootRunId = parentRun.RootRunId ?? parentRun.Id;

        WorkflowRun newRun = new()
        {
            Id = Guid.NewGuid(),
            ParentRunId = parentRunId,
            RootRunId = rootRunId,
            CreatedAt = DateTime.UtcNow,
            Status = WorkflowStatus.Pending,
            InputTextOriginal = parentRun.InputTextOriginal,
            InputTextRedacted = parentRun.InputTextRedacted,
            RerunFromStep = fromStep
        };

        await runRepo.CreateAsync(newRun, cancellationToken);

        int fromStepOrder = WorkflowSteps.GetStepOrder(fromStep);
        foreach (string stepName in WorkflowSteps.AllSteps)
        {
            int stepOrder = WorkflowSteps.GetStepOrder(stepName);
            WorkflowStepRun step = new()
            {
                Id = Guid.NewGuid(),
                RunId = newRun.Id,
                StepName = stepName,
                StepOrder = stepOrder,
                Status = stepOrder < fromStepOrder ? StepStatus.Skipped : StepStatus.Pending,
                ConcurrencyGroup = WorkflowSteps.IsConcurrentStep(stepName)
                    ? WorkflowSteps.ConcurrencyGroupName
                    : null
            };
            await stepRepo.CreateAsync(step, cancellationToken);
        }

        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningWorkflows[newRun.Id] = linkedCts;

        _ = ExecuteWorkflowAsync(newRun.Id, fromStep, linkedCts.Token);

        return newRun;
    }

    public async Task CancelRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (_runningWorkflows.TryRemove(runId, out CancellationTokenSource? cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();

        WorkflowRun? run = await runRepo.GetByIdAsync(runId, cancellationToken);
        if (run is { Status: WorkflowStatus.Running })
        {
            run.Status = WorkflowStatus.Canceled;
            run.CompletedAt = DateTime.UtcNow;
            await runRepo.UpdateAsync(run, cancellationToken);
        }
    }

    private async Task ExecuteWorkflowAsync(Guid runId, string? startFromStep, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();
        var stepRepo = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        WorkflowRun? run = await runRepo.GetByIdAsync(runId, cancellationToken);
        if (run == null) return;

        try
        {
            run.Status = WorkflowStatus.Running;
            run.StartedAt = DateTime.UtcNow;
            await runRepo.UpdateAsync(run, cancellationToken);

            OnboardingContext context = new()
            {
                NormalizedInput = run.InputTextRedacted ?? run.InputTextOriginal ?? string.Empty,
                OriginalInput = run.InputTextOriginal ?? string.Empty
            };

            if (!string.IsNullOrEmpty(startFromStep) && run.ParentRunId.HasValue)
            {
                context = await ReconstructContextFromParentAsync(
                    run.ParentRunId.Value, startFromStep, scope, cancellationToken);
            }

            int startFromOrder = string.IsNullOrEmpty(startFromStep)
                ? 1
                : WorkflowSteps.GetStepOrder(startFromStep);

            // Phase 1: Sequential steps before concurrent stage
            foreach (string stepName in new[] { WorkflowSteps.IntakeNormalize, WorkflowSteps.ExtractProfile })
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (WorkflowSteps.GetStepOrder(stepName) < startFromOrder) continue;
                context = await ExecuteSequentialStepAsync(runId, stepName, context, scope, cancellationToken);
            }

            // Phase 2: Concurrent review stage (MAF fan-out/fan-in)
            if (WorkflowSteps.GetStepOrder(WorkflowSteps.SecurityReview) >= startFromOrder)
            {
                context = await ExecuteConcurrentReviewStageAsync(runId, context, scope, cancellationToken);
            }

            // Phase 3: Sequential steps after concurrent stage
            foreach (string stepName in new[] { WorkflowSteps.CustomerNextSteps, WorkflowSteps.FinalPackage })
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (WorkflowSteps.GetStepOrder(stepName) < startFromOrder) continue;
                context = await ExecuteSequentialStepAsync(runId, stepName, context, scope, cancellationToken);
            }

            totalStopwatch.Stop();
            run.Status = WorkflowStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.TotalDurationMs = (int)totalStopwatch.ElapsedMilliseconds;
            run.FinalOutputHtml = context.FinalHtml;
            await runRepo.UpdateAsync(run, cancellationToken);
            await _progressHub.SendRunCompletedAsync(runId, true, null, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            run.Status = WorkflowStatus.Canceled;
            run.CompletedAt = DateTime.UtcNow;
            run.TotalDurationMs = (int)totalStopwatch.ElapsedMilliseconds;
            await runRepo.UpdateAsync(run, CancellationToken.None);
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            run.Status = WorkflowStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.TotalDurationMs = (int)totalStopwatch.ElapsedMilliseconds;
            run.Error = ex.Message;
            await runRepo.UpdateAsync(run, CancellationToken.None);
            await _progressHub.SendRunCompletedAsync(runId, false, ex.Message, CancellationToken.None);
        }
        finally
        {
            _runningWorkflows.TryRemove(runId, out _);
        }
    }

    /// <summary>
    /// Executes the concurrent review stage using the MAF WorkflowBuilder fan-out/fan-in pattern.
    /// Three reviewer agents run in parallel; the barrier waits for all to complete;
    /// the aggregation executor merges findings into a DecisionPack.
    /// </summary>
    private async Task<OnboardingContext> ExecuteConcurrentReviewStageAsync(
        Guid runId,
        OnboardingContext context,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var stepRepo = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();
        var llmClient = scope.ServiceProvider.GetRequiredService<ILlmClient>();

        Stopwatch stageStopwatch = Stopwatch.StartNew();

        // Mark all concurrent review steps as Running
        foreach (string reviewerStep in WorkflowSteps.ConcurrentReviewers)
        {
            WorkflowStepRun? step = await stepRepo.GetByRunIdAndStepNameAsync(runId, reviewerStep, cancellationToken);
            if (step != null)
            {
                step.Status = StepStatus.Running;
                step.StartedAt = DateTime.UtcNow;
                await stepRepo.UpdateAsync(step, cancellationToken);
                await _progressHub.SendStepStartedAsync(runId, reviewerStep, cancellationToken);
            }
        }

        await _progressHub.SendConcurrentGroupStartedAsync(runId, WorkflowSteps.ConcurrentReviewers, cancellationToken);

        // Build and run the MAF concurrent workflow
        Workflow mafWorkflow = ConcurrentReviewWorkflow.Build(_chatClient, llmClient);

        string decisionPackJson;
        await using (StreamingRun mafRun = await InProcessExecution.RunStreamingAsync(
            mafWorkflow, input: context.ApplicantProfileJson ?? "{}", cancellationToken: cancellationToken))
        {
            string? output = null;
            await foreach (WorkflowEvent evt in mafRun.WatchStreamAsync(cancellationToken))
            {
                if (evt is WorkflowOutputEvent outputEvt)
                {
                    output = outputEvt.Data?.ToString();
                }
                else if (evt is SuperStepCompletedEvent superStepEvt)
                {
                    // Emit progress for completed supersteps
                    await _progressHub.SendStepStatusAsync(
                        runId, "ConcurrentReview",
                        $"Superstep {superStepEvt.StepNumber} completed",
                        null, cancellationToken);
                }
            }

            decisionPackJson = output ?? "{}";
        }

        stageStopwatch.Stop();
        int durationMs = (int)stageStopwatch.ElapsedMilliseconds;

        // Mark all concurrent review steps as Completed
        foreach (string reviewerStep in WorkflowSteps.ConcurrentReviewers)
        {
            WorkflowStepRun? step = await stepRepo.GetByRunIdAndStepNameAsync(runId, reviewerStep, cancellationToken);
            if (step != null)
            {
                step.Status = StepStatus.Completed;
                step.CompletedAt = DateTime.UtcNow;
                step.DurationMs = durationMs;
                await stepRepo.UpdateAsync(step, cancellationToken);
                await _progressHub.SendStepCompletedAsync(runId, reviewerStep, durationMs, cancellationToken);
            }
        }

        // Mark aggregation step
        WorkflowStepRun? aggStep = await stepRepo.GetByRunIdAndStepNameAsync(
            runId, WorkflowSteps.AggregateFindings, cancellationToken);
        if (aggStep != null)
        {
            aggStep.Status = StepStatus.Completed;
            aggStep.StartedAt = DateTime.UtcNow.AddMilliseconds(-durationMs);
            aggStep.CompletedAt = DateTime.UtcNow;
            aggStep.DurationMs = durationMs;

            TruncationResult outputTruncation = SnapshotTruncator.Truncate(decisionPackJson);
            aggStep.OutputSnapshot = outputTruncation.Text;
            aggStep.OutputIsTruncated = outputTruncation.IsTruncated;
            aggStep.OutputFullLength = outputTruncation.FullLength;

            await stepRepo.UpdateAsync(aggStep, cancellationToken);
            await _progressHub.SendStepCompletedAsync(
                runId, WorkflowSteps.AggregateFindings, durationMs, cancellationToken);
        }

        await _progressHub.SendBarrierReleasedAsync(runId, cancellationToken);

        // Update context with the concurrent stage results
        context.DecisionPackJson = decisionPackJson;

        return context;
    }

    private async Task<OnboardingContext> ExecuteSequentialStepAsync(
        Guid runId,
        string stepName,
        OnboardingContext context,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var stepRepo = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();
        Stopwatch stepStopwatch = Stopwatch.StartNew();

        WorkflowStepRun? step = await stepRepo.GetByRunIdAndStepNameAsync(runId, stepName, cancellationToken)
            ?? throw new InvalidOperationException($"Step {stepName} not found for run {runId}.");

        step.Status = StepStatus.Running;
        step.StartedAt = DateTime.UtcNow;
        await stepRepo.UpdateAsync(step, cancellationToken);
        await _progressHub.SendStepStartedAsync(runId, stepName, cancellationToken);

        string inputJson = context.ToJson();
        TruncationResult inputTruncation = SnapshotTruncator.Truncate(inputJson);
        step.InputSnapshot = inputTruncation.Text;
        step.InputIsTruncated = inputTruncation.IsTruncated;
        step.InputFullLength = inputTruncation.FullLength;

        try
        {
            IStepExecutor executor = CreateExecutor(stepName, scope);
            context = await executor.ExecuteAsync(context, cancellationToken);

            string outputJson = context.ToJson();
            TruncationResult outputTruncation = SnapshotTruncator.Truncate(outputJson);
            step.OutputSnapshot = outputTruncation.Text;
            step.OutputIsTruncated = outputTruncation.IsTruncated;
            step.OutputFullLength = outputTruncation.FullLength;

            if (context.Warnings.Count > 0)
            {
                step.WarningsJson = JsonSerializer.Serialize(context.Warnings);
            }

            stepStopwatch.Stop();
            step.Status = StepStatus.Completed;
            step.CompletedAt = DateTime.UtcNow;
            step.DurationMs = (int)stepStopwatch.ElapsedMilliseconds;
            await stepRepo.UpdateAsync(step, cancellationToken);
            await _progressHub.SendStepCompletedAsync(runId, stepName, (int)stepStopwatch.ElapsedMilliseconds, cancellationToken);

            return context;
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            step.Status = StepStatus.Failed;
            step.CompletedAt = DateTime.UtcNow;
            step.DurationMs = (int)stepStopwatch.ElapsedMilliseconds;
            step.Error = ex.Message;
            await stepRepo.UpdateAsync(step, CancellationToken.None);
            await _progressHub.SendStepFailedAsync(runId, stepName, ex.Message, CancellationToken.None);
            throw;
        }
    }

    private async Task<OnboardingContext> ReconstructContextFromParentAsync(
        Guid parentRunId,
        string fromStep,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var stepRepo = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();
        int fromStepOrder = WorkflowSteps.GetStepOrder(fromStep);

        IReadOnlyList<WorkflowStepRun> completedSteps =
            await stepRepo.GetCompletedStepsBeforeAsync(parentRunId, fromStepOrder, cancellationToken);

        WorkflowStepRun? lastCompleted = completedSteps
            .Where(s => s.StepOrder < fromStepOrder && !string.IsNullOrEmpty(s.OutputSnapshot))
            .OrderByDescending(s => s.StepOrder)
            .FirstOrDefault();

        if (lastCompleted?.OutputSnapshot != null)
        {
            try
            {
                return OnboardingContext.FromJson(lastCompleted.OutputSnapshot);
            }
            catch (JsonException) { }
        }

        return new OnboardingContext();
    }

    private static IStepExecutor CreateExecutor(string stepName, IServiceScope scope)
    {
        return stepName switch
        {
            WorkflowSteps.IntakeNormalize => new IntakeNormalizeExecutor(),
            WorkflowSteps.ExtractProfile => new ExtractProfileExecutor(
                scope.ServiceProvider.GetRequiredService<ILlmClient>()),
            WorkflowSteps.CustomerNextSteps => new CustomerNextStepsExecutor(
                scope.ServiceProvider.GetRequiredService<ILlmClient>()),
            WorkflowSteps.FinalPackage => new FinalPackageExecutor(),
            _ => throw new InvalidOperationException($"Unknown sequential step: {stepName}")
        };
    }
}
