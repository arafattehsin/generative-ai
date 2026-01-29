// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Step 1: Intake and normalize the input text.
/// This is a non-LLM step that cleans up the input.
/// </summary>
public sealed partial class IntakeNormalizeExecutor : IStepExecutor
{
    /// <inheritdoc />
    public string StepName => WorkflowSteps.IntakeNormalize;

    /// <inheritdoc />
    public Task<WorkflowContext> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default)
    {
        string input = context.NormalizedInput;

        // Normalize line endings
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");

        // Remove excessive blank lines (more than 2 consecutive)
        input = ExcessiveNewlinesRegex().Replace(input, "\n\n");

        // Normalize whitespace (multiple spaces to single)
        input = MultipleSpacesRegex().Replace(input, " ");

        // Trim each line
        string[] lines = input.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
        }
        input = string.Join("\n", lines);

        // Remove control characters except newlines and tabs
        input = ControlCharsRegex().Replace(input, "");

        // Trim overall
        input = input.Trim();

        // Validation
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
