// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;
using OnboardFlow.Domain.ValueObjects;

namespace OnboardFlow.Application.Executors;

/// <summary>
/// Step 1: Intake and normalize the input text.
/// Non-LLM step — cleans whitespace, validates input.
/// </summary>
public sealed partial class IntakeNormalizeExecutor : IStepExecutor
{
    public string StepName => WorkflowSteps.IntakeNormalize;

    public Task<OnboardingContext> ExecuteAsync(OnboardingContext context, CancellationToken cancellationToken = default)
    {
        string input = context.NormalizedInput;

        // Normalize line endings
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");

        // Remove excessive blank lines
        input = ExcessiveNewlinesRegex().Replace(input, "\n\n");

        // Normalize whitespace
        input = MultipleSpacesRegex().Replace(input, " ");

        // Trim each line
        string[] lines = input.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
        }
        input = string.Join("\n", lines);

        // Remove control characters
        input = ControlCharsRegex().Replace(input, "");

        input = input.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            context.Warnings.Add("Input text is empty after normalization.");
        }
        else if (input.Length < 50)
        {
            context.Warnings.Add("Input text is very short (less than 50 characters). Results may be limited.");
        }

        context.NormalizedInput = input;

        return Task.FromResult(context);
    }

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewlinesRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F]")]
    private static partial Regex ControlCharsRegex();
}
