// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace OnboardFlow.Application.Executors.Maf;

/// <summary>
/// MAF workflow entry point. Receives the applicant profile JSON and fans it out
/// to all concurrent reviewer agents by sending a ChatMessage + TurnToken.
/// </summary>
internal sealed partial class ReviewStartExecutor() : Executor("ReviewStart")
{
    [MessageHandler]
    public async ValueTask HandleAsync(string profileJson, IWorkflowContext context, CancellationToken ct = default)
    {
        string prompt = $"""
            Review the following applicant profile for onboarding approval.
            Provide your assessment as a structured JSON object.

            --- APPLICANT PROFILE ---
            {profileJson}
            --- END ---
            """;

        await context.SendMessageAsync(new ChatMessage(ChatRole.User, prompt), ct);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), ct);
    }
}
