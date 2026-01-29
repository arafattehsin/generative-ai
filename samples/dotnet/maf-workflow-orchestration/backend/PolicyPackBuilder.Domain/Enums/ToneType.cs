// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Domain.Enums;

/// <summary>
/// Represents the desired tone for the policy output.
/// </summary>
public enum ToneType
{
    /// <summary>
    /// Formal, authoritative, business-appropriate tone.
    /// </summary>
    Professional = 0,

    /// <summary>
    /// Warm, approachable, conversational but respectful tone.
    /// </summary>
    Friendly = 1,

    /// <summary>
    /// Highly structured, official, ceremonial tone.
    /// </summary>
    Formal = 2
}
