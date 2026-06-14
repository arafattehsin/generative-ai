// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;
using OnboardRoom.Application.Interfaces;

namespace OnboardRoom.Infrastructure.Services;

public sealed partial class RegexPiiRedactor : IPiiRedactor
{
    public string Redact(string text)
    {
        string redacted = EmailRegex().Replace(text, "[email-redacted]");
        redacted = PhoneRegex().Replace(redacted, "[phone-redacted]");
        return redacted;
    }

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<!\w)(?:\+?\d[\d\s().-]{7,}\d)(?!\w)")]
    private static partial Regex PhoneRegex();
}
