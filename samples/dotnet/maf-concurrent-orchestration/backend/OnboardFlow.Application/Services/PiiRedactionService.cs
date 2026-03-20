// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;
using OnboardFlow.Application.Interfaces;

namespace OnboardFlow.Application.Services;

/// <summary>
/// Detects and redacts PII from text before LLM processing.
/// </summary>
public sealed partial class PiiRedactionService : IPiiRedactionService
{
    public RedactionResult Redact(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new RedactionResult
            {
                OriginalText = input ?? string.Empty,
                RedactedText = input ?? string.Empty
            };
        }

        List<RedactedItemInfo> redactedItems = [];
        string redactedText = input;
        int emailCounter = 1;
        int phoneCounter = 1;

        MatchCollection emailMatches = EmailRegex().Matches(input);
        foreach (Match match in emailMatches)
        {
            string placeholder = $"[EMAIL_{emailCounter}]";
            redactedItems.Add(new RedactedItemInfo
            {
                Type = "email",
                OriginalValue = match.Value,
                Placeholder = placeholder,
                Position = match.Index
            });
            redactedText = redactedText.Replace(match.Value, placeholder);
            emailCounter++;
        }

        MatchCollection phoneMatches = PhoneRegex().Matches(input);
        foreach (Match match in phoneMatches)
        {
            string placeholder = $"[PHONE_{phoneCounter}]";
            redactedItems.Add(new RedactedItemInfo
            {
                Type = "phone",
                OriginalValue = match.Value,
                Placeholder = placeholder,
                Position = match.Index
            });
            redactedText = redactedText.Replace(match.Value, placeholder);
            phoneCounter++;
        }

        return new RedactionResult
        {
            OriginalText = input,
            RedactedText = redactedText,
            RedactedItems = redactedItems
        };
    }

    [GeneratedRegex(@"\b[\w.-]+@[\w.-]+\.\w{2,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b")]
    private static partial Regex PhoneRegex();
}
