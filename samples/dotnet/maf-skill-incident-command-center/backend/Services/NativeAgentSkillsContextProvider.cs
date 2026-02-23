using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace IncidentCommandCenter.Api.Services;

public sealed class NativeAgentSkillsContextProvider(
    ISkillCatalogService skillCatalog,
    ISkillTraceStore traceStore,
    SkillRunContextAccessor runContextAccessor,
    ILogger<NativeAgentSkillsContextProvider> logger) : AIContextProvider
{
    private readonly AITool _loadSkillTool = AIFunctionFactory.Create((string skillName) =>
        LoadSkill(skillCatalog, traceStore, runContextAccessor, logger, skillName));

    private readonly AITool _readSkillResourceTool = AIFunctionFactory.Create((string skillName, string resourcePath) =>
        ReadSkillResource(skillCatalog, traceStore, runContextAccessor, logger, skillName, resourcePath));

    public override ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Models.SkillSummary> skills = skillCatalog.GetSkills();
        string advertisedSkills = string.Join("\n", skills.Select(skill => $"- {skill.Name}: {skill.Description}"));

        AIContext aiContext = new()
        {
            Instructions = $$"""
You have access to native Skill tools. Use them when the task requires domain-specific policy, templates, or references.

Available skills:
{{advertisedSkills}}

Tool usage protocol:
1. Call load_skill(skillName) to fetch full SKILL.md instructions.
2. Call read_skill_resource(skillName, resourcePath) for references/assets linked by the skill.
3. Follow skill rules before final answer.
""",
            Tools = [_loadSkillTool, _readSkillResourceTool],
        };

        logger.LogInformation(
            "Injected native skill tools into AI context. SkillCount={SkillCount}, RunId={RunId}",
            skills.Count,
            runContextAccessor.CurrentRunId ?? "<none>");

        _ = context;
        _ = cancellationToken;
        return ValueTask.FromResult(aiContext);
    }

    public override ValueTask InvokedAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = cancellationToken;
        return ValueTask.CompletedTask;
    }

    private static string LoadSkill(
        ISkillCatalogService skillCatalog,
        ISkillTraceStore traceStore,
        SkillRunContextAccessor runContextAccessor,
        ILogger<NativeAgentSkillsContextProvider> logger,
        string skillName)
    {
        string resolvedSkill = skillCatalog.ResolveSkillName(skillName);
        string content = skillCatalog.ReadSkillDefinition(resolvedSkill);
        logger.LogInformation(
            "Native tool call: load_skill. Requested={RequestedSkill}, Resolved={ResolvedSkill}, RunId={RunId}",
            skillName,
            resolvedSkill,
            runContextAccessor.CurrentRunId ?? "<none>");

        string? runId = runContextAccessor.CurrentRunId;
        if (!string.IsNullOrWhiteSpace(runId))
        {
            traceStore.AddEvent(SkillEventFactory.Create(runId, "loaded", resolvedSkill, null,
                "Skill loaded through native load_skill tool."));
        }

        return content;
    }

    private static string ReadSkillResource(
        ISkillCatalogService skillCatalog,
        ISkillTraceStore traceStore,
        SkillRunContextAccessor runContextAccessor,
        ILogger<NativeAgentSkillsContextProvider> logger,
        string skillName,
        string resourcePath)
    {
        string resolvedSkill = skillCatalog.ResolveSkillName(skillName);
        string content = skillCatalog.ReadResource(resolvedSkill, resourcePath);
        logger.LogInformation(
            "Native tool call: read_skill_resource. Skill={ResolvedSkill}, Resource={ResourcePath}, RunId={RunId}",
            resolvedSkill,
            resourcePath,
            runContextAccessor.CurrentRunId ?? "<none>");

        string? runId = runContextAccessor.CurrentRunId;
        if (!string.IsNullOrWhiteSpace(runId))
        {
            traceStore.AddEvent(SkillEventFactory.Create(runId, "resource_read", resolvedSkill, resourcePath,
                "Resource loaded through native read_skill_resource tool."));
        }

        return content;
    }
}
