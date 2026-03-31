// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Application.Executors;
using OnboardFlow.Application.Interfaces;
using OnboardFlow.Application.Orchestration;
using OnboardFlow.Domain.Entities;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RunsController : ControllerBase
{
    private readonly OnboardFlowOrchestrator _orchestrator;
    private readonly IWorkflowRunRepository _runRepository;
    private readonly IWorkflowStepRunRepository _stepRepository;

    public RunsController(
        OnboardFlowOrchestrator orchestrator,
        IWorkflowRunRepository runRepository,
        IWorkflowStepRunRepository stepRepository)
    {
        _orchestrator = orchestrator;
        _runRepository = runRepository;
        _stepRepository = stepRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<RunSummaryDto>>> GetRuns(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        List<WorkflowRun> runs = await _runRepository.GetAllAsync(skip, take, cancellationToken);

        List<RunSummaryDto> summaries = [];
        foreach (WorkflowRun run in runs)
        {
            List<WorkflowStepRun> steps = await _stepRepository.GetByRunIdAsync(run.Id, cancellationToken);
            summaries.Add(MapToSummary(run, steps));
        }

        return Ok(summaries);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RunDetailDto>> GetRun(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run == null) return NotFound();

        List<WorkflowStepRun> steps = await _stepRepository.GetByRunIdAsync(id, cancellationToken);
        return Ok(MapToDetail(run, steps));
    }

    [HttpPost]
    public async Task<ActionResult<CreateRunResponseDto>> CreateRun(
        [FromBody] CreateRunRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InputText))
            return BadRequest("Input text is required.");

        WorkflowRun run = await _orchestrator.StartRunAsync(request.InputText, cancellationToken);

        return AcceptedAtAction(nameof(GetRun), new { id = run.Id }, new CreateRunResponseDto
        {
            RunId = run.Id,
            Status = run.Status.ToString(),
            CreatedAt = run.CreatedAt
        });
    }

    [HttpPost("{id:guid}/rerun")]
    public async Task<ActionResult<RerunResponseDto>> RerunFromStep(
        Guid id,
        [FromBody] RerunRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FromStep))
            return BadRequest("FromStep is required.");

        if (!WorkflowSteps.AllSteps.Contains(request.FromStep))
            return BadRequest($"Invalid step. Valid steps: {string.Join(", ", WorkflowSteps.AllSteps)}");

        WorkflowRun? existing = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound();

        WorkflowRun newRun = await _orchestrator.RerunFromStepAsync(id, request.FromStep, cancellationToken);

        return AcceptedAtAction(nameof(GetRun), new { id = newRun.Id }, new RerunResponseDto
        {
            NewRunId = newRun.Id,
            ParentRunId = id,
            FromStep = request.FromStep,
            Status = newRun.Status.ToString(),
            CreatedAt = newRun.CreatedAt
        });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelRun(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run == null) return NotFound();

        await _orchestrator.CancelRunAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/lineage")]
    public async Task<ActionResult<List<RunSummaryDto>>> GetLineage(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run == null) return NotFound();

        Guid rootId = run.RootRunId ?? run.Id;
        List<WorkflowRun> lineage = await _runRepository.GetLineageAsync(rootId, cancellationToken);

        List<RunSummaryDto> summaries = [];
        foreach (WorkflowRun r in lineage)
        {
            List<WorkflowStepRun> steps = await _stepRepository.GetByRunIdAsync(r.Id, cancellationToken);
            summaries.Add(MapToSummary(r, steps));
        }
        return Ok(summaries);
    }

    [HttpGet("steps")]
    public ActionResult<List<StepDefinitionDto>> GetStepDefinitions()
    {
        var definitions = WorkflowSteps.AllSteps.Select(s => new StepDefinitionDto
        {
            Name = s,
            Order = WorkflowSteps.GetStepOrder(s),
            Description = WorkflowSteps.GetStepDescription(s),
            IsConcurrent = WorkflowSteps.IsConcurrentStep(s),
            ConcurrencyGroup = WorkflowSteps.IsConcurrentStep(s) ? WorkflowSteps.ConcurrencyGroupName : null
        }).ToList();

        return Ok(definitions);
    }

    private static RunSummaryDto MapToSummary(WorkflowRun run, List<WorkflowStepRun> steps) => new()
    {
        Id = run.Id,
        Status = run.Status.ToString(),
        CreatedAt = run.CreatedAt,
        CompletedAt = run.CompletedAt,
        TotalDurationMs = run.TotalDurationMs,
        ParentRunId = run.ParentRunId,
        RerunFromStep = run.RerunFromStep,
        StepCount = steps.Count,
        CompletedStepCount = steps.Count(s => s.Status == Domain.Enums.StepStatus.Completed),
        Error = run.Error
    };

    private static RunDetailDto MapToDetail(WorkflowRun run, List<WorkflowStepRun> steps) => new()
    {
        Id = run.Id,
        Status = run.Status.ToString(),
        CreatedAt = run.CreatedAt,
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        TotalDurationMs = run.TotalDurationMs,
        ParentRunId = run.ParentRunId,
        RootRunId = run.RootRunId,
        RerunFromStep = run.RerunFromStep,
        InputTextRedacted = run.InputTextRedacted,
        FinalOutputHtml = run.FinalOutputHtml,
        Error = run.Error,
        Steps = steps.Select(s => new StepDto
        {
            Id = s.Id,
            StepName = s.StepName,
            StepOrder = s.StepOrder,
            Status = s.Status.ToString(),
            ConcurrencyGroup = s.ConcurrencyGroup,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            DurationMs = s.DurationMs,
            OutputSnapshot = s.OutputSnapshot,
            Error = s.Error
        }).ToList()
    };
}

// --- DTOs ---

public sealed record CreateRunRequestDto
{
    public string InputText { get; init; } = string.Empty;
}

public sealed record CreateRunResponseDto
{
    public Guid RunId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record RerunRequestDto
{
    public string FromStep { get; init; } = string.Empty;
}

public sealed record RerunResponseDto
{
    public Guid NewRunId { get; init; }
    public Guid ParentRunId { get; init; }
    public string FromStep { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record RunSummaryDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public long? TotalDurationMs { get; init; }
    public Guid? ParentRunId { get; init; }
    public string? RerunFromStep { get; init; }
    public int StepCount { get; init; }
    public int CompletedStepCount { get; init; }
    public string? Error { get; init; }
}

public sealed record RunDetailDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public long? TotalDurationMs { get; init; }
    public Guid? ParentRunId { get; init; }
    public Guid? RootRunId { get; init; }
    public string? RerunFromStep { get; init; }
    public string? InputTextRedacted { get; init; }
    public string? FinalOutputHtml { get; init; }
    public string? Error { get; init; }
    public List<StepDto> Steps { get; init; } = [];
}

public sealed record StepDto
{
    public Guid Id { get; init; }
    public string StepName { get; init; } = string.Empty;
    public int StepOrder { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ConcurrencyGroup { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public long? DurationMs { get; init; }
    public string? OutputSnapshot { get; init; }
    public string? Error { get; init; }
}

public sealed record StepDefinitionDto
{
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsConcurrent { get; init; }
    public string? ConcurrencyGroup { get; init; }
}
