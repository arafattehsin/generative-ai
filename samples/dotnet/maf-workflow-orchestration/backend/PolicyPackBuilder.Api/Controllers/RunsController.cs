// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using PolicyPackBuilder.Application.DTOs;
using PolicyPackBuilder.Application.Executors;
using PolicyPackBuilder.Application.Interfaces;
using PolicyPackBuilder.Application.Orchestration;
using PolicyPackBuilder.Domain.Entities;
using PolicyPackBuilder.Domain.Enums;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Api.Controllers;

/// <summary>
/// API controller for workflow runs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RunsController : ControllerBase
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly IWorkflowRunRepository _runRepository;
    private readonly IWorkflowStepRunRepository _stepRepository;

    /// <summary>
    /// Initializes a new instance of the RunsController.
    /// </summary>
    public RunsController(
        IWorkflowOrchestrator orchestrator,
        IWorkflowRunRepository runRepository,
        IWorkflowStepRunRepository stepRepository)
    {
        _orchestrator = orchestrator;
        _runRepository = runRepository;
        _stepRepository = stepRepository;
    }

    /// <summary>
    /// Gets all workflow runs with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<RunSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<RunSummaryResponse>>> GetRuns(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        List<WorkflowRun> runs = await _runRepository.GetAllAsync(skip, take, cancellationToken);
        int total = await _runRepository.GetTotalCountAsync(cancellationToken);

        List<RunSummaryResponse> summaries = [];
        foreach (WorkflowRun run in runs)
        {
            List<WorkflowStepRun> steps = await _stepRepository.GetByRunIdAsync(run.Id, cancellationToken);
            summaries.Add(MapToSummaryResponse(run, steps));
        }

        return Ok(new PaginatedResponse<RunSummaryResponse>
        {
            Items = summaries,
            Total = total,
            Skip = skip,
            Take = take
        });
    }

    /// <summary>
    /// Gets a specific workflow run by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunResponse>> GetRun(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run == null)
        {
            return NotFound();
        }

        List<WorkflowStepRun> steps = await _stepRepository.GetByRunIdAsync(id, cancellationToken);
        return Ok(MapToRunResponse(run, steps));
    }

    /// <summary>
    /// Creates a new workflow run.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRunResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateRunResponse>> CreateRun(
        [FromBody] CreateRunRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InputText))
        {
            return BadRequest("Input text is required.");
        }

        WorkflowOptions options = new()
        {
            Audience = request.Options?.Audience ?? AudienceType.Customer,
            Tone = request.Options?.Tone ?? ToneType.Professional,
            StrictCompliance = request.Options?.StrictCompliance ?? false
        };

        WorkflowRun run = await _orchestrator.StartRunAsync(
            request.InputText,
            options.ToJson(),
            cancellationToken);

        return AcceptedAtAction(nameof(GetRun), new { id = run.Id }, new CreateRunResponse
        {
            RunId = run.Id,
            Status = run.Status.ToString(),
            CreatedAt = run.CreatedAt
        });
    }

    /// <summary>
    /// Re-runs a workflow from a specific step.
    /// </summary>
    [HttpPost("{id:guid}/rerun")]
    [ProducesResponseType(typeof(RerunResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RerunResponse>> RerunFromStep(
        Guid id,
        [FromBody] RerunRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FromStep))
        {
            return BadRequest("FromStep is required.");
        }

        if (!WorkflowSteps.AllSteps.Contains(request.FromStep))
        {
            return BadRequest($"Invalid step name. Valid steps are: {string.Join(", ", WorkflowSteps.AllSteps)}");
        }

        WorkflowRun? existingRun = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (existingRun == null)
        {
            return NotFound();
        }

        WorkflowRun newRun = await _orchestrator.RerunFromStepAsync(id, request.FromStep, cancellationToken);

        return AcceptedAtAction(nameof(GetRun), new { id = newRun.Id }, new RerunResponse
        {
            NewRunId = newRun.Id,
            ParentRunId = id,
            FromStep = request.FromStep,
            Status = newRun.Status.ToString(),
            CreatedAt = newRun.CreatedAt
        });
    }

    /// <summary>
    /// Cancels a running workflow.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRun(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run == null)
        {
            return NotFound();
        }

        await _orchestrator.CancelRunAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the lineage of a workflow run (all related runs).
    /// </summary>
    [HttpGet("{id:guid}/lineage")]
    [ProducesResponseType(typeof(List<RunSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RunSummaryResponse>>> GetLineage(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run == null)
        {
            return NotFound();
        }

        Guid rootId = run.RootRunId ?? run.Id;
        List<WorkflowRun> lineage = await _runRepository.GetLineageAsync(rootId, cancellationToken);

        List<RunSummaryResponse> summaries = [];
        foreach (WorkflowRun lineageRun in lineage)
        {
            List<WorkflowStepRun> steps = await _stepRepository.GetByRunIdAsync(lineageRun.Id, cancellationToken);
            summaries.Add(MapToSummaryResponse(lineageRun, steps));
        }

        return Ok(summaries);
    }

    /// <summary>
    /// Gets step definitions for the workflow.
    /// </summary>
    [HttpGet("steps")]
    [ProducesResponseType(typeof(List<StepDefinition>), StatusCodes.Status200OK)]
    public ActionResult<List<StepDefinition>> GetStepDefinitions()
    {
        List<StepDefinition> definitions = WorkflowSteps.AllSteps
            .Select(s => new StepDefinition
            {
                Name = s,
                Order = WorkflowSteps.GetStepOrder(s),
                Description = WorkflowSteps.GetStepDescription(s),
                UsesLlm = WorkflowSteps.UsesLlm(s)
            })
            .ToList();

        return Ok(definitions);
    }

    private static RunResponse MapToRunResponse(WorkflowRun run, List<WorkflowStepRun> steps)
    {
        WorkflowOptions? options = null;
        if (!string.IsNullOrEmpty(run.OptionsJson))
        {
            try
            {
                options = WorkflowOptions.FromJson(run.OptionsJson);
            }
            catch
            {
                // Ignore parse errors
            }
        }

        return new RunResponse
        {
            Id = run.Id,
            ParentRunId = run.ParentRunId,
            RootRunId = run.RootRunId,
            Status = run.Status.ToString(),
            CreatedAt = run.CreatedAt,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            TotalDurationMs = run.TotalDurationMs,
            InputTextRedacted = run.InputTextRedacted,
            FinalOutputHtml = run.FinalOutputHtml,
            Error = run.Error,
            RerunFromStep = run.RerunFromStep,
            Options = options != null ? new RunOptionsDto
            {
                Audience = options.Audience,
                Tone = options.Tone,
                StrictCompliance = options.StrictCompliance
            } : null,
            Steps = steps.Select(MapToStepResponse).ToList()
        };
    }

    private static RunSummaryResponse MapToSummaryResponse(WorkflowRun run, List<WorkflowStepRun> steps)
    {
        WorkflowOptions? options = null;
        if (!string.IsNullOrEmpty(run.OptionsJson))
        {
            try
            {
                options = WorkflowOptions.FromJson(run.OptionsJson);
            }
            catch
            {
                // Ignore parse errors
            }
        }

        int completedSteps = steps.Count(s => s.Status == StepStatus.Completed);
        int totalSteps = steps.Count;

        return new RunSummaryResponse
        {
            Id = run.Id,
            ParentRunId = run.ParentRunId,
            Status = run.Status.ToString(),
            CreatedAt = run.CreatedAt,
            CompletedAt = run.CompletedAt,
            TotalDurationMs = run.TotalDurationMs,
            RerunFromStep = run.RerunFromStep,
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps,
            Options = options != null ? new RunOptionsDto
            {
                Audience = options.Audience,
                Tone = options.Tone,
                StrictCompliance = options.StrictCompliance
            } : null
        };
    }

    private static StepResponse MapToStepResponse(WorkflowStepRun step)
    {
        return new StepResponse
        {
            Id = step.Id,
            StepName = step.StepName,
            StepOrder = step.StepOrder,
            Status = step.Status.ToString(),
            StartedAt = step.StartedAt,
            CompletedAt = step.CompletedAt,
            DurationMs = step.DurationMs,
            InputSnapshot = step.InputSnapshot,
            InputIsTruncated = step.InputIsTruncated,
            InputFullLength = step.InputFullLength,
            OutputSnapshot = step.OutputSnapshot,
            OutputIsTruncated = step.OutputIsTruncated,
            OutputFullLength = step.OutputFullLength,
            WarningsJson = step.WarningsJson,
            Error = step.Error
        };
    }
}

/// <summary>
/// Request to re-run a workflow from a specific step.
/// </summary>
public sealed class RerunRequest
{
    /// <summary>
    /// The step to re-run from.
    /// </summary>
    public string FromStep { get; set; } = string.Empty;
}

/// <summary>
/// Paginated response wrapper.
/// </summary>
public sealed class PaginatedResponse<T>
{
    /// <summary>
    /// The items in this page.
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// Total count of items.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Number of items skipped.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Number of items taken.
    /// </summary>
    public int Take { get; set; }
}

/// <summary>
/// Step definition for UI.
/// </summary>
public sealed class StepDefinition
{
    /// <summary>
    /// Step name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Step order (1-based).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Step description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this step uses LLM.
    /// </summary>
    public bool UsesLlm { get; set; }
}
