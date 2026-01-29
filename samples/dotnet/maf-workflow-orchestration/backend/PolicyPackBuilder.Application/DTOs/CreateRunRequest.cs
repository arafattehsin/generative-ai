// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Domain.Enums;

namespace PolicyPackBuilder.Application.DTOs;

/// <summary>
/// Request to create a new workflow run.
/// </summary>
public sealed class CreateRunRequest
{
    /// <summary>
    /// The input text to process.
    /// </summary>
    public string InputText { get; set; } = string.Empty;

    /// <summary>
    /// Workflow options.
    /// </summary>
    public RunOptionsDto Options { get; set; } = new();
}

/// <summary>
/// Options for a workflow run.
/// </summary>
public sealed class RunOptionsDto
{
    /// <summary>
    /// Target audience.
    /// </summary>
    public AudienceType Audience { get; set; } = AudienceType.Customer;

    /// <summary>
    /// Desired tone.
    /// </summary>
    public ToneType Tone { get; set; } = ToneType.Professional;

    /// <summary>
    /// Whether to enforce strict compliance.
    /// </summary>
    public bool StrictCompliance { get; set; } = false;
}
