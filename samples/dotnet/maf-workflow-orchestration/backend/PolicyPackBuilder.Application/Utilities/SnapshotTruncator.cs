// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Application.Utilities;

/// <summary>
/// Utility for truncating large text snapshots to prevent database bloat.
/// </summary>
public static class SnapshotTruncator
{
    /// <summary>
    /// Default maximum length for snapshots (50,000 characters).
    /// </summary>
    public const int DefaultMaxLength = 50000;

    /// <summary>
    /// Marker appended to truncated text.
    /// </summary>
    public const string TruncationMarker = "...[TRUNCATED]";

    /// <summary>
    /// Truncates text if it exceeds the maximum length.
    /// </summary>
    /// <param name="text">The text to potentially truncate.</param>
    /// <param name="maxLength">Maximum allowed length.</param>
    /// <returns>Truncation result with metadata.</returns>
    public static TruncationResult Truncate(string? text, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new TruncationResult
            {
                Text = text ?? string.Empty,
                IsTruncated = false,
                FullLength = text?.Length ?? 0
            };
        }

        int fullLength = text.Length;

        if (fullLength <= maxLength)
        {
            return new TruncationResult
            {
                Text = text,
                IsTruncated = false,
                FullLength = fullLength
            };
        }

        // Truncate and add marker
        int truncateAt = maxLength - TruncationMarker.Length;
        string truncatedText = text[..truncateAt] + TruncationMarker;

        return new TruncationResult
        {
            Text = truncatedText,
            IsTruncated = true,
            FullLength = fullLength
        };
    }
}

/// <summary>
/// Result of a truncation operation.
/// </summary>
public sealed class TruncationResult
{
    /// <summary>
    /// The potentially truncated text.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Whether the text was truncated.
    /// </summary>
    public bool IsTruncated { get; init; }

    /// <summary>
    /// Original length before truncation.
    /// </summary>
    public int FullLength { get; init; }
}
