using Microsoft.AspNetCore.Mvc;
using TravelConcierge.Api.Services;

namespace TravelConcierge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SamplesController(SampleDataService samples) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(samples.GetSamples());
}
