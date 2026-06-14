// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using OnboardRoom.Infrastructure.Services;

namespace OnboardRoom.Tests;

public sealed class PiiRedactorTests
{
    [Fact]
    public void Redact_masks_email_and_phone_number()
    {
        RegexPiiRedactor redactor = new();

        string result = redactor.Redact("Contact Jamie at jamie@example.com or +61 412 345 678.");

        result.Should().Contain("[email-redacted]");
        result.Should().Contain("[phone-redacted]");
        result.Should().NotContain("jamie@example.com");
        result.Should().NotContain("+61 412 345 678");
    }
}
