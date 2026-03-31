// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Application.Utilities;

/// <summary>
/// Truncates large JSON snapshots to stay within SQLite storage limits.
/// </summary>
public static class SnapshotTruncator
{
    private const int MaxLength = 500_000;

    public static TruncationResult Truncate(string text)
    {
        if (text.Length <= MaxLength)
        {
            return new TruncationResult
            {
                Text = text,
                IsTruncated = false,
                FullLength = text.Length
            };
        }

        return new TruncationResult
        {
            Text = text[..MaxLength],
            IsTruncated = true,
            FullLength = text.Length
        };
    }
}

public sealed class TruncationResult
{
    public string Text { get; set; } = string.Empty;
    public bool IsTruncated { get; set; }
    public int FullLength { get; set; }
}
