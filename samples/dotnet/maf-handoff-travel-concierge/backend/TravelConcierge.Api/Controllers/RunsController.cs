using Microsoft.AspNetCore.Mvc;
using TravelConcierge.Api.Models;
using TravelConcierge.Api.Services;

namespace TravelConcierge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RunsController(RunStore store, RunCoordinator coordinator) : ControllerBase
{
    [HttpGet]
    public IActionResult GetRuns() => Ok(store.GetAll());

    [HttpGet("{runId:guid}")]
    public IActionResult GetRun(Guid runId)
    {
        TravelRun? run = store.Get(runId);
        return run == null ? NotFound() : Ok(run);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRun(CreateRunRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        Guid runId = await coordinator.StartRunAsync(request, cancellationToken).ConfigureAwait(false);
        return Accepted(new RunCreatedResponse(runId));
    }

    [HttpPost("{runId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid runId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        TravelRun? run = store.Get(runId);
        if (run is not null && run.Status is RunStatus.WaitingForHuman or RunStatus.HumanResponding)
        {
            return Conflict("A recovery specialist is reviewing this case. The traveller can reply after the review.");
        }

        bool accepted = await coordinator.SendMessageAsync(runId, request, cancellationToken).ConfigureAwait(false);
        return accepted ? Accepted() : NotFound();
    }

    [HttpPost("{runId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid runId, CancellationToken cancellationToken)
    {
        bool accepted = await coordinator.CancelAsync(runId, cancellationToken).ConfigureAwait(false);
        return accepted ? Accepted() : NotFound();
    }
}
