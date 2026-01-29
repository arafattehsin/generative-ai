// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PolicyPackBuilder.Application.Executors;
using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Application.Utilities;
using PolicyPackBuilder.Domain.Entities;
using PolicyPackBuilder.Domain.Enums;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Orchestration;

/// <summary>
/// Orchestrates the sequential execution of workflow steps.
/// Persists step outputs to SQLite for re-run support.
/// Emits SignalR events for real-time progress.
/// </summary>
public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWorkflowProgressHub _progressHub;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningWorkflows = new();

    /// <summary>
    /// Initializes a new instance of the WorkflowOrchestrator.
    /// </summary>
    public WorkflowOrchestrator(
        IServiceScopeFactory scopeFactory,
        IWorkflowProgressHub progressHub)
    {
        _scopeFactory = scopeFactory;
        _progressHub = progressHub;
    }

    /// <inheritdoc />
    public async Task<WorkflowRun> StartRunAsync(string inputText, string optionsJson, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkflowRunRepository runRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();
        IWorkflowStepRunRepository stepRepository = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();
        IPiiRedactionService piiRedactionService = scope.ServiceProvider.GetRequiredService<IPiiRedactionService>();

        // Create the run entity
        WorkflowRun run = new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = WorkflowStatus.Pending,
            OptionsJson = optionsJson,
            InputTextOriginal = inputText
        };

        // Redact PII from input
        RedactionResult redactionResult = piiRedactionService.Redact(inputText);
        run.InputTextRedacted = redactionResult.RedactedText;

        // Persist the run
        await runRepository.CreateAsync(run, cancellationToken);

        // Create step entities
        foreach (string stepName in WorkflowSteps.AllSteps)
        {
            WorkflowStepRun step = new()
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                StepName = stepName,
                StepOrder = WorkflowSteps.GetStepOrder(stepName),
                Status = StepStatus.Pending
            };
            await stepRepository.CreateAsync(step, cancellationToken);
        }

        // Start execution in background
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningWorkflows[run.Id] = linkedCts;

        _ = ExecuteWorkflowAsync(run.Id, null, linkedCts.Token);

        return run;
    }

    /// <inheritdoc />
    public async Task<WorkflowRun> RerunFromStepAsync(Guid parentRunId, string fromStep, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkflowRunRepository runRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();
        IWorkflowStepRunRepository stepRepository = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();

        WorkflowRun? parentRun = await runRepository.GetByIdAsync(parentRunId, cancellationToken);
        if (parentRun == null)
        {
            throw new InvalidOperationException($"Parent run {parentRunId} not found.");
        }

        // Determine root run ID (for lineage tracking)
        Guid rootRunId = parentRun.RootRunId ?? parentRun.Id;

        // Create new run with lineage
        WorkflowRun newRun = new()
        {
            Id = Guid.NewGuid(),
            ParentRunId = parentRunId,
            RootRunId = rootRunId,
            CreatedAt = DateTime.UtcNow,
            Status = WorkflowStatus.Pending,
            OptionsJson = parentRun.OptionsJson,
            InputTextOriginal = parentRun.InputTextOriginal,
            InputTextRedacted = parentRun.InputTextRedacted,
            RerunFromStep = fromStep
        };

        await runRepository.CreateAsync(newRun, cancellationToken);

        // Create step entities
        int fromStepOrder = WorkflowSteps.GetStepOrder(fromStep);
        foreach (string stepName in WorkflowSteps.AllSteps)
        {
            int stepOrder = WorkflowSteps.GetStepOrder(stepName);
            StepStatus initialStatus = stepOrder < fromStepOrder ? StepStatus.Skipped : StepStatus.Pending;

            WorkflowStepRun step = new()
            {
                Id = Guid.NewGuid(),
                RunId = newRun.Id,
                StepName = stepName,
                StepOrder = stepOrder,
                Status = initialStatus
            };
            await stepRepository.CreateAsync(step, cancellationToken);
        }

        // Start execution
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningWorkflows[newRun.Id] = linkedCts;

        _ = ExecuteWorkflowAsync(newRun.Id, fromStep, linkedCts.Token);

        return newRun;
    }

    /// <inheritdoc />
    public async Task CancelRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (_runningWorkflows.TryRemove(runId, out CancellationTokenSource? cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkflowRunRepository runRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();

        WorkflowRun? run = await runRepository.GetByIdAsync(runId, cancellationToken);
        if (run != null && run.Status == WorkflowStatus.Running)
        {
            run.Status = WorkflowStatus.Canceled;
            run.CompletedAt = DateTime.UtcNow;
            await runRepository.UpdateAsync(run, cancellationToken);
        }
    }

    private async Task ExecuteWorkflowAsync(Guid runId, string? startFromStep, CancellationToken cancellationToken)
    {
        // Create a new scope for this background task to avoid disposed DbContext
        using IServiceScope scope = _scopeFactory.CreateScope();
        IWorkflowRunRepository runRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRunRepository>();
        IWorkflowStepRunRepository stepRepository = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        WorkflowRun? run = await runRepository.GetByIdAsync(runId, cancellationToken);

        if (run == null)
        {
            return;
        }

        try
        {
            // Update status to running
            run.Status = WorkflowStatus.Running;
            run.StartedAt = DateTime.UtcNow;
            await runRepository.UpdateAsync(run, cancellationToken);

            // Parse options
            WorkflowOptions options = string.IsNullOrEmpty(run.OptionsJson)
                ? new WorkflowOptions()
                : WorkflowOptions.FromJson(run.OptionsJson);

            // Initialize context
            WorkflowContext context = new()
            {
                NormalizedInput = run.InputTextRedacted ?? run.InputTextOriginal ?? string.Empty,
                Options = options,
                RedactedItems = []
            };

            // If re-running, load previous context from parent run
            if (!string.IsNullOrEmpty(startFromStep) && run.ParentRunId.HasValue)
            {
                context = await ReconstructContextFromParentAsync(run.ParentRunId.Value, startFromStep, context, scope, cancellationToken);
            }

            // Determine which steps to execute
            int startFromOrder = string.IsNullOrEmpty(startFromStep) ? 1 : WorkflowSteps.GetStepOrder(startFromStep);

            // Execute each step
            foreach (string stepName in WorkflowSteps.AllSteps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int stepOrder = WorkflowSteps.GetStepOrder(stepName);

                if (stepOrder < startFromOrder)
                {
                    continue; // Skip steps before re-run point
                }

                context = await ExecuteStepAsync(runId, stepName, context, scope, cancellationToken);
            }

            // Complete the run
            totalStopwatch.Stop();
            run.Status = WorkflowStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.TotalDurationMs = (int)totalStopwatch.ElapsedMilliseconds;
            run.FinalOutputHtml = context.FinalHtml;
            await runRepository.UpdateAsync(run, cancellationToken);

            // Emit completion event
            await _progressHub.SendRunCompletedAsync(runId, true, null, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            run.Status = WorkflowStatus.Canceled;
            run.CompletedAt = DateTime.UtcNow;
            run.TotalDurationMs = (int)totalStopwatch.ElapsedMilliseconds;
            await runRepository.UpdateAsync(run, CancellationToken.None);
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            run.Status = WorkflowStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.TotalDurationMs = (int)totalStopwatch.ElapsedMilliseconds;
            run.Error = ex.Message;
            await runRepository.UpdateAsync(run, CancellationToken.None);

            await _progressHub.SendRunCompletedAsync(runId, false, ex.Message, CancellationToken.None);
        }
        finally
        {
            _runningWorkflows.TryRemove(runId, out _);
        }
    }

    private async Task<WorkflowContext> ExecuteStepAsync(
        Guid runId,
        string stepName,
        WorkflowContext context,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        IWorkflowStepRunRepository stepRepository = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();
        Stopwatch stepStopwatch = Stopwatch.StartNew();

        // Get the step entity
        WorkflowStepRun? step = await stepRepository.GetByRunIdAndStepNameAsync(runId, stepName, cancellationToken);
        if (step == null)
        {
            throw new InvalidOperationException($"Step {stepName} not found for run {runId}.");
        }

        // Update step status
        step.Status = StepStatus.Running;
        step.StartedAt = DateTime.UtcNow;
        await stepRepository.UpdateAsync(step, cancellationToken);

        // Emit step started event
        await _progressHub.SendStepStartedAsync(runId, stepName, cancellationToken);

        // Emit initial status
        await _progressHub.SendStepStatusAsync(runId, stepName, $"🔧 Preparing {stepName}...", 0, cancellationToken);

        // Capture input snapshot
        string inputJson = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
        TruncationResult inputTruncation = SnapshotTruncator.Truncate(inputJson);
        step.InputSnapshot = inputTruncation.Text;
        step.InputIsTruncated = inputTruncation.IsTruncated;
        step.InputFullLength = inputTruncation.FullLength;

        try
        {
            // Get the executor for this step
            await _progressHub.SendStepStatusAsync(runId, stepName, $"⚡ Initializing executor for {stepName}...", 10, cancellationToken);
            IStepExecutor executor = GetExecutor(stepName, scope);

            Console.WriteLine($"[ORCH] Executing step: {stepName}");
            // Execute the step
            await _progressHub.SendStepStatusAsync(runId, stepName, $"🤖 Executing {stepName} with LLM...", 30, cancellationToken);
            context = await executor.ExecuteAsync(context, cancellationToken);
            Console.WriteLine($"[ORCH] Step {stepName} executor completed");

            // Capture output snapshot
            await _progressHub.SendStepStatusAsync(runId, stepName, $"💾 Processing results from {stepName}...", 70, cancellationToken);
            Console.WriteLine($"[ORCH] Serializing output for {stepName}");
            string outputJson = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"[ORCH] Serialization complete for {stepName}");
            TruncationResult outputTruncation = SnapshotTruncator.Truncate(outputJson);
            step.OutputSnapshot = outputTruncation.Text;
            step.OutputIsTruncated = outputTruncation.IsTruncated;
            step.OutputFullLength = outputTruncation.FullLength;

            // Capture warnings
            if (context.Warnings.Count > 0)
            {
                step.WarningsJson = JsonSerializer.Serialize(context.Warnings);
            }

            // Complete the step
            await _progressHub.SendStepStatusAsync(runId, stepName, $"✅ Saving {stepName} results...", 90, cancellationToken);
            Console.WriteLine($"[ORCH] Completing step {stepName}");
            stepStopwatch.Stop();
            step.Status = StepStatus.Completed;
            step.CompletedAt = DateTime.UtcNow;
            step.DurationMs = (int)stepStopwatch.ElapsedMilliseconds;

            try
            {
                await stepRepository.UpdateAsync(step, cancellationToken);
                Console.WriteLine($"[ORCH] Step {stepName} saved to database");
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"[ORCH ERROR] Failed to save step {stepName}: {dbEx.Message}");
                throw;
            }

            // Emit step completed event
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
            await stepRepository.UpdateAsync(step, CancellationToken.None);

            // Emit step failed event
            await _progressHub.SendStepFailedAsync(runId, stepName, ex.Message, CancellationToken.None);

            throw;
        }
    }

    private async Task<WorkflowContext> ReconstructContextFromParentAsync(
        Guid parentRunId,
        string fromStep,
        WorkflowContext baseContext,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        IWorkflowStepRunRepository stepRepository = scope.ServiceProvider.GetRequiredService<IWorkflowStepRunRepository>();

        // Get completed steps from parent run before the re-run point
        int fromStepOrder = WorkflowSteps.GetStepOrder(fromStep);
        IReadOnlyList<WorkflowStepRun> completedSteps = await stepRepository.GetCompletedStepsBeforeAsync(
            parentRunId, fromStepOrder, cancellationToken);

        // Find the step right before the re-run point
        WorkflowStepRun? lastCompletedStep = completedSteps
            .Where(s => s.StepOrder < fromStepOrder && !string.IsNullOrEmpty(s.OutputSnapshot))
            .OrderByDescending(s => s.StepOrder)
            .FirstOrDefault();

        if (lastCompletedStep?.OutputSnapshot != null)
        {
            try
            {
                WorkflowContext? reconstructed = JsonSerializer.Deserialize<WorkflowContext>(lastCompletedStep.OutputSnapshot);
                if (reconstructed != null)
                {
                    // Preserve options from base context
                    reconstructed.Options = baseContext.Options;
                    return reconstructed;
                }
            }
            catch (JsonException)
            {
                // Failed to deserialize, use base context
            }
        }

        return baseContext;
    }

    private IStepExecutor GetExecutor(string stepName, IServiceScope scope)
    {
        return stepName switch
        {
            WorkflowSteps.IntakeNormalize => new IntakeNormalizeExecutor(),
            WorkflowSteps.ExtractFacts => new ExtractFactsExecutor(GetService<ILlmClient>(scope)),
            WorkflowSteps.DraftSummary => new DraftSummaryExecutor(GetService<ILlmClient>(scope)),
            WorkflowSteps.ComplianceCheck => new ComplianceCheckExecutor(GetService<ILlmClient>(scope), GetService<IComplianceRulesEngine>(scope)),
            WorkflowSteps.BrandToneRewrite => new BrandToneRewriteExecutor(GetService<ILlmClient>(scope)),
            WorkflowSteps.FinalPackage => new FinalPackageExecutor(),
            _ => throw new InvalidOperationException($"Unknown step: {stepName}")
        };
    }

    private T GetService<T>(IServiceScope scope) where T : notnull
    {
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
