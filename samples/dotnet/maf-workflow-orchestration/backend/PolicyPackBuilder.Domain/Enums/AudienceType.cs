// Copyright (c) Microsoft. All rights reserved.

namespace PolicyPackBuilder.Domain.Enums;

/// <summary>
/// Represents the target audience for the policy output.
/// </summary>
public enum AudienceType
{
    /// <summary>
    /// External customer-facing communication.
    /// </summary>
    Customer = 0,

    /// <summary>
    /// Internal organizational communication.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Legal or compliance-focused communication.
    /// </summary>
    Legal = 2
}
