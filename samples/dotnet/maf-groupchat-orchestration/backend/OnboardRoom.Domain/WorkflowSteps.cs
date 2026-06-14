// Copyright (c) Microsoft. All rights reserved.

namespace OnboardRoom.Domain;

public static class WorkflowSteps
{
    public const string IntakeNormalize = nameof(IntakeNormalize);
    public const string ExtractProfile = nameof(ExtractProfile);
    public const string BoardroomReview = nameof(BoardroomReview);
    public const string ChairRecommendation = nameof(ChairRecommendation);
    public const string CustomerNextSteps = nameof(CustomerNextSteps);
    public const string FinalPackage = nameof(FinalPackage);

    public static readonly IReadOnlyList<string> All =
    [
        IntakeNormalize,
        ExtractProfile,
        BoardroomReview,
        ChairRecommendation,
        CustomerNextSteps,
        FinalPackage,
    ];

    public static int OrderOf(string stepName) => Array.IndexOf(All.ToArray(), stepName) + 1;

    public static string DescriptionOf(string stepName) => stepName switch
    {
        IntakeNormalize => "Redact PII and convert the request into a stable room brief.",
        ExtractProfile => "Extract applicant, region, urgency, start date, needs, and open questions.",
        BoardroomReview => "Run the Foundry-hosted multi-agent group chat with a chair-led manager.",
        ChairRecommendation => "Parse the chair response into decision, confidence, owners, blockers, and next actions.",
        CustomerNextSteps => "Format the response for the requester and implementation owners.",
        FinalPackage => "Create the exportable HTML package used by the UI.",
        _ => "Unknown step",
    };
}
