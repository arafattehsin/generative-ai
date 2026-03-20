// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Application.Interfaces;

/// <summary>
/// PII detection and redaction service.
/// </summary>
public interface IPiiRedactionService
{
    RedactionResult Redact(string input);
}

public sealed class RedactionResult
{
    public string OriginalText { get; set; } = string.Empty;
    public string RedactedText { get; set; } = string.Empty;
    public List<RedactedItemInfo> RedactedItems { get; set; } = [];
}

public sealed class RedactedItemInfo
{
    public string Type { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public int Position { get; set; }
}
