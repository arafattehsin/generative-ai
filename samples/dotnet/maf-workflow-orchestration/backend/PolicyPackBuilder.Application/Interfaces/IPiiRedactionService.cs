// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Application.Interfaces;

/// <summary>
/// Service for detecting and redacting PII (Personally Identifiable Information).
/// </summary>
public interface IPiiRedactionService
{
    /// <summary>
    /// Redacts PII from the input text.
    /// </summary>
    /// <param name="input">The text to scan for PII.</param>
    /// <returns>Result containing redacted text and list of redacted items.</returns>
    RedactionResult Redact(string input);
}

/// <summary>
/// Result of a PII redaction operation.
/// </summary>
public sealed class RedactionResult
{
    /// <summary>
    /// The text with PII replaced by placeholders.
    /// </summary>
    public string RedactedText { get; init; } = string.Empty;

    /// <summary>
    /// The original unmodified text.
    /// </summary>
    public string OriginalText { get; init; } = string.Empty;

    /// <summary>
    /// List of items that were redacted.
    /// </summary>
    public List<RedactedItemInfo> RedactedItems { get; init; } = [];

    /// <summary>
    /// Whether any PII was found and redacted.
    /// </summary>
    public bool HasRedactions => RedactedItems.Count > 0;
}

/// <summary>
/// Information about a single redacted PII item.
/// </summary>
public sealed class RedactedItemInfo
{
    /// <summary>
    /// Type of PII (e.g., "email", "phone").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Original value that was redacted.
    /// </summary>
    public string OriginalValue { get; init; } = string.Empty;

    /// <summary>
    /// Placeholder used in redacted text.
    /// </summary>
    public string Placeholder { get; init; } = string.Empty;

    /// <summary>
    /// Position in original text.
    /// </summary>
    public int Position { get; init; }
}
