// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Application.DTOs;

/// <summary>
/// Response containing step run details.
/// </summary>
public sealed class StepResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Step name.
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Order in workflow (1-based).
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Input snapshot.
    /// </summary>
    public string? InputSnapshot { get; set; }

    /// <summary>
    /// Whether input was truncated.
    /// </summary>
    public bool InputIsTruncated { get; set; }

    /// <summary>
    /// Full input length.
    /// </summary>
    public int? InputFullLength { get; set; }

    /// <summary>
    /// Output snapshot.
    /// </summary>
    public string? OutputSnapshot { get; set; }

    /// <summary>
    /// Whether output was truncated.
    /// </summary>
    public bool OutputIsTruncated { get; set; }

    /// <summary>
    /// Full output length.
    /// </summary>
    public int? OutputFullLength { get; set; }

    /// <summary>
    /// JSON-serialized warnings.
    /// </summary>
    public string? WarningsJson { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? Error { get; set; }
}
