// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using OnboardRoom.Application.Dtos;
using OnboardRoom.Application.Interfaces;
using OnboardRoom.Application.Services;
using OnboardRoom.Domain;
using OnboardRoom.Domain.Entities;

namespace OnboardRoom.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RunsController(
    IRunRepository repository,
    RunOrchestrator orchestrator,
    IServiceScopeFactory scopeFactory,
    ILogger<RunsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RunSummaryDto>>> GetRuns([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkflowRun> runs = await repository.ListAsync(skip, take, cancellationToken);
        return this.Ok(runs.Select(RunMapper.ToSummary).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RunDetailDto>> GetRun(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await repository.GetAsync(id, cancellationToken);
        return run is null ? this.NotFound() : this.Ok(RunMapper.ToDetail(run));
    }

    [HttpPost]
    public async Task<ActionResult<StartRunResponse>> StartRun([FromBody] StartRunRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InputText))
        {
            return this.BadRequest("InputText is required.");
        }

        WorkflowRun run = await orchestrator.CreateRunAsync(request, cancellationToken);
        _ = Task.Run(async () => await ExecuteRunInBackgroundAsync(run.Id), CancellationToken.None);

        return this.AcceptedAtAction(
            nameof(GetRun),
            new { id = run.Id },
            new StartRunResponse(run.Id, run.Status.ToString(), run.CreatedAt));
    }

    [HttpPost("{id:guid}/rerun")]
    public async Task<ActionResult<RerunResponse>> Rerun(Guid id, [FromBody] RerunRequest request, CancellationToken cancellationToken = default)
    {
        if (!WorkflowSteps.All.Contains(request.FromStep))
        {
            return this.BadRequest($"Invalid step. Valid values: {string.Join(", ", WorkflowSteps.All)}");
        }

        WorkflowRun? existing = await repository.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return this.NotFound();
        }

        WorkflowRun rerun = await orchestrator.RerunFromStepAsync(id, request.FromStep, cancellationToken);
        _ = Task.Run(async () => await ExecuteRunInBackgroundAsync(rerun.Id), CancellationToken.None);

        return this.AcceptedAtAction(
            nameof(GetRun),
            new { id = rerun.Id },
            new RerunResponse(rerun.Id, id, request.FromStep, rerun.Status.ToString(), rerun.CreatedAt));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await repository.GetAsync(id, cancellationToken);
        if (run is null)
        {
            return this.NotFound();
        }

        await orchestrator.CancelAsync(id, cancellationToken);
        return this.NoContent();
    }

    [HttpGet("{id:guid}/lineage")]
    public async Task<ActionResult<IReadOnlyList<RunSummaryDto>>> GetLineage(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await repository.GetAsync(id, cancellationToken);
        if (run is null)
        {
            return this.NotFound();
        }

        Guid rootId = run.RootRunId ?? run.Id;
        IReadOnlyList<WorkflowRun> lineage = await repository.LineageAsync(rootId, cancellationToken);
        return this.Ok(lineage.Select(RunMapper.ToSummary).ToList());
    }

    [HttpGet("steps")]
    public ActionResult<IReadOnlyList<StepDefinitionDto>> GetSteps()
        => this.Ok(WorkflowSteps.All.Select(step => new StepDefinitionDto(
            step,
            WorkflowSteps.OrderOf(step),
            WorkflowSteps.DescriptionOf(step),
            step == WorkflowSteps.BoardroomReview)).ToList());

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await repository.GetAsync(id, cancellationToken);
        if (run is null)
        {
            return this.NotFound();
        }

        return this.Content(run.FinalOutputHtml ?? "<article><h1>Run has no export yet.</h1></article>", "text/html");
    }

    private async Task ExecuteRunInBackgroundAsync(Guid runId)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            RunOrchestrator scopedOrchestrator = scope.ServiceProvider.GetRequiredService<RunOrchestrator>();
            await scopedOrchestrator.ExecuteAsync(runId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnboardRoom run {RunId} failed.", runId);
        }
    }
}
