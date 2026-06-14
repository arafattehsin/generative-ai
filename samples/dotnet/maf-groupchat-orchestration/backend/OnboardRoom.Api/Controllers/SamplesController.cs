// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using OnboardRoom.Application.Dtos;
using OnboardRoom.Application.Services;

namespace OnboardRoom.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SamplesController(SampleDataService sampleDataService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<SampleRequestDto>> GetSamples()
        => this.Ok(sampleDataService.GetSamples());
}
