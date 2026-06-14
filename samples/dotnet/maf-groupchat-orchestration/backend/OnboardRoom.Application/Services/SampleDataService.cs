// Copyright (c) Microsoft. All rights reserved.

using OnboardRoom.Application.Dtos;

namespace OnboardRoom.Application.Services;

public sealed class SampleDataService
{
    public IReadOnlyList<SampleRequestDto> GetSamples() =>
    [
        new(
            "Sydney field engineer",
            "AU",
            "High",
            """
            New employee onboarding request:
            - Persona: senior field engineer joining Contoso Energy
            - Location: Sydney, Australia
            - Start date: next Monday
            - Needs: laptop, VPN, GitHub access, field safety training, benefits orientation, and customer site access
            Coordinate the onboarding plan, identify risks, and produce the chair's final action list.
            """),
        new(
            "EU product manager",
            "EU",
            "Normal",
            """
            New hire onboarding request:
            - Persona: product manager joining Fabrikam Cloud
            - Location: Berlin, Germany
            - Start date: July 1
            - Needs: Microsoft 365, Jira, GitHub read access, product analytics, GDPR handling briefing, mentor assignment
            Please identify approvals, dependencies, and a first-week plan.
            """),
        new(
            "US contractor conversion",
            "US",
            "Critical",
            """
            Internal conversion request:
            - Persona: support contractor converting to full-time identity engineer
            - Location: Redmond, WA
            - Start date: in 3 business days
            - Needs: device swap, access cleanup, privileged access review, benefits session, security training, team onboarding
            Highlight risk if the access transition is not ready before day one.
            """),
    ];
}
