// Copyright (c) Microsoft. All rights reserved.

namespace OnboardRoom.Domain.Entities;

public sealed class GroupChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RunId { get; set; }

    public WorkflowRun? Run { get; set; }

    public string Speaker { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
