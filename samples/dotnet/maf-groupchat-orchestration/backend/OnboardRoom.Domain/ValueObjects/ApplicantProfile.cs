// Copyright (c) Microsoft. All rights reserved.

namespace OnboardRoom.Domain.ValueObjects;

public sealed record ApplicantProfile(
    string Name,
    string Role,
    string Region,
    string StartDate,
    string Urgency,
    IReadOnlyList<string> Needs,
    IReadOnlyList<string> Unknowns)
{
    public static ApplicantProfile Empty { get; } = new(
        Name: "Unknown applicant",
        Role: "Unknown role",
        Region: "Unknown region",
        StartDate: "Unknown",
        Urgency: "Normal",
        Needs: [],
        Unknowns: []);
}
