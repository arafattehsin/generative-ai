using Microsoft.AspNetCore.Mvc;
using TravelConcierge.Api.Models;
using TravelConcierge.Api.Services;

namespace TravelConcierge.Api.Controllers;

[ApiController]
[Route("api/operations")]
public sealed class OperationsController(HumanSupportWorkflowService humanSupport) : ControllerBase
{
    [HttpGet("cases")]
    public IActionResult GetCases() => Ok(humanSupport.GetQueue());

    [HttpPost("cases/{runId:guid}/claim")]
    public async Task<IActionResult> Claim(
        Guid runId,
        ClaimHumanSupportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OperatorName))
        {
            return BadRequest("Operator name is required.");
        }

        bool claimed = await humanSupport.ClaimAsync(runId, request.OperatorName, cancellationToken).ConfigureAwait(false);
        return claimed ? Accepted() : Conflict("This case is no longer available to claim.");
    }

    [HttpPost("cases/{runId:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid runId,
        ResolveHumanSupportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OperatorName) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Operator name and customer response are required.");
        }

        HumanSupportResolution resolution = new(
            request.OperatorName.Trim(),
            request.Message.Trim(),
            request.NextOwnerId);
        bool resolved = await humanSupport.ResolveAsync(runId, resolution, cancellationToken).ConfigureAwait(false);
        return resolved ? Accepted() : Conflict("Claim this case before sending a response.");
    }
}
