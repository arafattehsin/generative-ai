// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Application.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SamplesController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<SampleRequest>> GetSamples()
        => Ok(SampleDataService.Samples);

    [HttpGet("{index:int}")]
    public ActionResult<SampleRequest> GetSample(int index)
    {
        if (index < 0 || index >= SampleDataService.Samples.Count)
            return NotFound();

        return Ok(SampleDataService.Samples[index]);
    }
}
