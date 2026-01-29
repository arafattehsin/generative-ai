// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Application.DTOs;

/// <summary>
/// A sample input for demo purposes.
/// </summary>
public sealed class SampleResponse
{
    /// <summary>
    /// Sample identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the sample.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The sample input text.
    /// </summary>
    public string InputText { get; set; } = string.Empty;
}
