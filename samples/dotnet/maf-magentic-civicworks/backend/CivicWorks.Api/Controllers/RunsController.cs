using CivicWorks.Api.Models;
using CivicWorks.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CivicWorks.Api.Controllers;

[ApiController]
[Route("api/runs")]
public sealed class RunsController(
    CivicWorksRunStore store,
    LiveMagenticRunService liveRuns) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CivicWorksRunSnapshot>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<CivicWorksRunSnapshot> Start()
    {
        try
        {
            CivicWorksRunSnapshot snapshot = liveRuns.StartRun();
            return AcceptedAtAction(nameof(Get), new { id = snapshot.Id }, snapshot);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Live Microsoft Foundry configuration required",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CivicWorksRunSnapshot>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CivicWorksRunSnapshot> Get(Guid id)
    {
        return store.TryGet(id, out CivicWorksRunSnapshot? snapshot) && snapshot is not null
            ? Ok(snapshot)
            : NotFound();
    }

    [HttpPost("{id:guid}/plan-review")]
    [ProducesResponseType<CivicWorksRunSnapshot>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CivicWorksRunSnapshot>> ReviewPlan(
        Guid id,
        [FromBody] PlanReviewCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            CivicWorksRunSnapshot snapshot = await liveRuns
                .ReviewPlanAsync(id, command, cancellationToken)
                .ConfigureAwait(false);
            return Ok(snapshot);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid plan-review command",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "The live workflow is not awaiting this decision",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
            });
        }
    }
}
