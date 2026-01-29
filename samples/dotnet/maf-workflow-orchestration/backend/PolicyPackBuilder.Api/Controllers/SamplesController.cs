// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using PolicyPackBuilder.Application.DTOs;
using PolicyPackBuilder.Application.Services;

namespace PolicyPackBuilder.Api.Controllers;

/// <summary>
/// API controller for sample policy drafts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SamplesController : ControllerBase
{
    /// <summary>
    /// Gets all available sample policy drafts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SampleResponse>), StatusCodes.Status200OK)]
    public ActionResult<List<SampleResponse>> GetSamples()
    {
        List<SampleResponse> samples = SampleDataService.GetSamples();
        return Ok(samples);
    }

    /// <summary>
    /// Gets a specific sample by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SampleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SampleResponse> GetSample(string id)
    {
        SampleResponse? sample = SampleDataService.GetSampleById(id);
        if (sample == null)
        {
            return NotFound();
        }

        return Ok(sample);
    }
}
