// Copyright (c) Microsoft. All rights reserved.

namespace OnboardRoom.Application.Interfaces;

public interface IPiiRedactor
{
    string Redact(string text);
}
