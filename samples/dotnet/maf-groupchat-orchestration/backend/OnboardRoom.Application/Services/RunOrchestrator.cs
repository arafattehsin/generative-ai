// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Text;
using System.Text.Json;
using OnboardRoom.Application.Dtos;
using OnboardRoom.Application.Interfaces;
using OnboardRoom.Domain;
using OnboardRoom.Domain.Entities;
using OnboardRoom.Domain.Enums;
using OnboardRoom.Domain.ValueObjects;

namespace OnboardRoom.Application.Services;

public sealed class RunOrchestrator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly IRunRepository _repository;
    private readonly IPiiRedactor _piiRedactor;
    private readonly IGroupChatWorkflowRunner _groupChatRunner;
    private readonly IRunEventSink _eventSink;

    public RunOrchestrator(
        IRunRepository repository,
        IPiiRedactor piiRedactor,
        IGroupChatWorkflowRunner groupChatRunner,
        IRunEventSink eventSink)
    {
        this._repository = repository;
        this._piiRedactor = piiRedactor;
        this._groupChatRunner = groupChatRunner;
        this._eventSink = eventSink;
    }

    public async Task<WorkflowRun> CreateRunAsync(StartRunRequest request, CancellationToken cancellationToken = default)
    {
        WorkflowRun run = new()
        {
            InputTextOriginal = request.InputText,
            InputTextRedacted = this._piiRedactor.Redact(request.InputText),
            Region = request.Region,
            Urgency = request.Urgency,
            MaxRounds = Math.Clamp(request.MaxRounds, 4, 10),
            Tone = request.Tone,
            Status = RunStatus.Pending,
        };

        foreach (string stepName in WorkflowSteps.All)
        {
            run.Steps.Add(new WorkflowStepRun
            {
                RunId = run.Id,
                StepName = stepName,
                StepOrder = WorkflowSteps.OrderOf(stepName),
                Status = StepStatus.Pending,
            });
        }

        await this._repository.AddAsync(run, cancellationToken);
        await this._repository.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task ExecuteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        WorkflowRun run = await this._repository.GetAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Run {runId} was not found.");

        try
        {
            run.Status = RunStatus.Running;
            run.StartedAt = DateTime.UtcNow;
            await this._repository.SaveChangesAsync(cancellationToken);

            string normalized = await ExecuteStepAsync(run, WorkflowSteps.IntakeNormalize, () =>
                Task.FromResult(BuildRoomBrief(run)), cancellationToken);

            ApplicantProfile profile = await ExecuteStepAsync(run, WorkflowSteps.ExtractProfile, () =>
                Task.FromResult(ExtractProfile(run, normalized)), cancellationToken);
            run.ProfileJson = JsonSerializer.Serialize(profile, SerializerOptions);

            GroupChatResult groupChat = await ExecuteStepAsync(run, WorkflowSteps.BoardroomReview, async () =>
                await this._groupChatRunner.RunAsync(run, normalized, this._eventSink, cancellationToken), cancellationToken);
            run.Messages.AddRange(groupChat.Messages);

            ChairRecommendation recommendation = await ExecuteStepAsync(run, WorkflowSteps.ChairRecommendation, () =>
                Task.FromResult(BuildRecommendation(groupChat, run)), cancellationToken);
            run.ChairRecommendationJson = JsonSerializer.Serialize(recommendation, SerializerOptions);

            string nextSteps = await ExecuteStepAsync(run, WorkflowSteps.CustomerNextSteps, () =>
                Task.FromResult(BuildCustomerNextSteps(profile, recommendation)), cancellationToken);

            run.FinalOutputHtml = await ExecuteStepAsync(run, WorkflowSteps.FinalPackage, () =>
                Task.FromResult(BuildFinalPackageHtml(run, profile, recommendation, nextSteps, groupChat)), cancellationToken);

            run.Status = RunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            await this._repository.SaveChangesAsync(cancellationToken);
            await this._eventSink.RunCompletedAsync(RunMapper.ToDetail(run), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            run.Status = RunStatus.Cancelled;
            run.CompletedAt = DateTime.UtcNow;
            await this._repository.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            run.Status = RunStatus.Failed;
            run.Error = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await this._repository.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<WorkflowRun> RerunFromStepAsync(Guid id, string fromStep, CancellationToken cancellationToken = default)
    {
        WorkflowRun source = await this._repository.GetAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Run {id} was not found.");

        WorkflowRun rerun = await this.CreateRunAsync(new StartRunRequest(
            source.InputTextOriginal,
            source.Region,
            source.Urgency,
            source.MaxRounds,
            source.Tone), cancellationToken);

        rerun.ParentRunId = source.Id;
        rerun.RootRunId = source.RootRunId ?? source.Id;
        rerun.RerunFromStep = fromStep;
        await this._repository.SaveChangesAsync(cancellationToken);
        return rerun;
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        WorkflowRun run = await this._repository.GetAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Run {id} was not found.");

        if (run.Status is RunStatus.Completed or RunStatus.Failed)
        {
            return;
        }

        run.Status = RunStatus.Cancelled;
        run.CompletedAt = DateTime.UtcNow;
        await this._repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<T> ExecuteStepAsync<T>(
        WorkflowRun run,
        string stepName,
        Func<Task<T>> work,
        CancellationToken cancellationToken)
    {
        WorkflowStepRun step = run.Steps.Single(candidate => candidate.StepName == stepName);
        step.Status = StepStatus.Running;
        step.StartedAt = DateTime.UtcNow;
        await this._repository.SaveChangesAsync(cancellationToken);
        await this._eventSink.StepStartedAsync(run.Id, step, cancellationToken);

        try
        {
            T result = await work();
            step.OutputSnapshot = Snapshot(result);
            step.Status = StepStatus.Completed;
            step.CompletedAt = DateTime.UtcNow;
            await this._repository.SaveChangesAsync(cancellationToken);
            await this._eventSink.StepCompletedAsync(run.Id, step, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            step.Status = StepStatus.Failed;
            step.Error = ex.Message;
            step.CompletedAt = DateTime.UtcNow;
            await this._repository.SaveChangesAsync(cancellationToken);
            await this._eventSink.StepFailedAsync(run.Id, step, cancellationToken);
            throw;
        }
    }

    private static string BuildRoomBrief(WorkflowRun run)
        => $"""
           Region: {run.Region}
           Urgency: {run.Urgency}
           Preferred tone: {run.Tone}
           Maximum boardroom rounds: {run.MaxRounds}

           Redacted request:
           {run.InputTextRedacted}
           """;

    private static ApplicantProfile ExtractProfile(WorkflowRun run, string brief)
    {
        IReadOnlyList<string> needs = brief.Split('\n')
            .Where(line => line.Contains("Needs:", StringComparison.OrdinalIgnoreCase) || line.TrimStart().StartsWith("-", StringComparison.Ordinal))
            .Select(line => line.Trim().TrimStart('-').Trim())
            .Where(line => line.Length > 0)
            .Take(8)
            .ToList();

        string role = brief.Contains("engineer", StringComparison.OrdinalIgnoreCase) ? "Engineer" :
            brief.Contains("manager", StringComparison.OrdinalIgnoreCase) ? "Manager" :
            "New hire";

        return new ApplicantProfile(
            Name: "Redacted applicant",
            Role: role,
            Region: run.Region,
            StartDate: brief.Contains("next Monday", StringComparison.OrdinalIgnoreCase) ? "Next Monday" : "See request",
            Urgency: run.Urgency,
            Needs: needs.Count > 0 ? needs : ["Device", "Identity", "Access", "Orientation"],
            Unknowns: ["Exact manager approver", "Final access group membership"]);
    }

    private static ChairRecommendation BuildRecommendation(GroupChatResult result, WorkflowRun run)
    {
        string chair = result.ChairResponse;
        string risk = chair.Contains("block", StringComparison.OrdinalIgnoreCase) || run.Urgency.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            ? "Elevated"
            : "Managed";

        return new ChairRecommendation(
            Decision: result.MaxRoundsReached ? "Proceed with chair follow-up" : "Proceed with tracked onboarding plan",
            RiskLevel: risk,
            Confidence: result.Messages.Count >= 4 ? 84 : 68,
            Owners: ["Hiring manager", "IT access owner", "People operations", "Security reviewer"],
            Blockers: result.MaxRoundsReached ? ["Boardroom hit maximum rounds before agreement"] : ["Approvals must be confirmed before day one"],
            Next48Hours: ["Confirm manager and cost center", "Provision device and identity", "Schedule benefits and safety sessions", "Validate access with least privilege"]);
    }

    private static string BuildCustomerNextSteps(ApplicantProfile profile, ChairRecommendation recommendation)
        => $"""
           Decision: {recommendation.Decision}
           Risk: {recommendation.RiskLevel}
           Region: {profile.Region}

           Next 48 hours:
           {string.Join(Environment.NewLine, recommendation.Next48Hours.Select(item => $"- {item}"))}
           """;

    private static string BuildFinalPackageHtml(
        WorkflowRun run,
        ApplicantProfile profile,
        ChairRecommendation recommendation,
        string nextSteps,
        GroupChatResult groupChat)
    {
        StringBuilder html = new();
        html.Append("<article class=\"onboardroom-export\">");
        html.Append($"<h1>{WebUtility.HtmlEncode(recommendation.Decision)}</h1>");
        html.Append($"<p><strong>Region:</strong> {WebUtility.HtmlEncode(profile.Region)} | <strong>Risk:</strong> {WebUtility.HtmlEncode(recommendation.RiskLevel)} | <strong>Confidence:</strong> {recommendation.Confidence}%</p>");
        html.Append("<h2>Next steps</h2><pre>");
        html.Append(WebUtility.HtmlEncode(nextSteps));
        html.Append("</pre><h2>Boardroom transcript</h2>");
        foreach (GroupChatMessage message in groupChat.Messages)
        {
            html.Append($"<section><h3>{WebUtility.HtmlEncode(message.Speaker)}</h3><p>{WebUtility.HtmlEncode(message.Content)}</p></section>");
        }

        html.Append("</article>");
        return html.ToString();
    }

    private static string Snapshot<T>(T value)
    {
        string text = value is string stringValue ? stringValue : JsonSerializer.Serialize(value, SerializerOptions);
        return text.Length > 1800 ? text[..1800] + "\n..." : text;
    }
}
