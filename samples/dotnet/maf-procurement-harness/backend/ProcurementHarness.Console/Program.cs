// Copyright (c) Arafat Tehsin. All rights reserved.
//
// Procurement Quote Review Harness
//
// A console-first sample that uses Microsoft Agent Framework Harness primitives to turn
// a procurement review into bounded, inspectable agent work:
// - todo and plan/execute context providers for multi-step work
// - file access over a controlled working directory
// - file-backed memory for notes created during the run
// - auto-approval rules that allow reads and safe output writes, but not deletes
// - Foundry-backed Responses client through AIProjectClient

#pragma warning disable OPENAI001
#pragma warning disable MAAI001
#pragma warning disable MEAI001

using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

SampleOptions options = SampleOptions.Parse(args);

if (options.ShowHelp)
{
    PrintHelp();
    return 0;
}

string sampleRoot = ResolveSampleRoot(options.WorkingDirectory);
string workingDirectory = Path.GetFullPath(options.WorkingDirectory ?? Path.Combine(sampleRoot, "working"));
Directory.CreateDirectory(Path.Combine(workingDirectory, "output"));

WorkspaceCheck check = CheckWorkspace(workingDirectory);
if (!check.IsValid)
{
    Console.Error.WriteLine("The procurement workspace is incomplete:");
    foreach (string missing in check.MissingFiles)
    {
        Console.Error.WriteLine($"  - {missing}");
    }

    return 1;
}

if (options.CheckOnly)
{
    PrintWorkspaceSummary(workingDirectory, check);
    PrintConfigurationSummary(options);
    return 0;
}

string endpoint = FirstNonEmpty(
        options.Endpoint,
        Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT"),
        Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT"))
    ?? throw new InvalidOperationException("Set FOUNDRY_PROJECT_ENDPOINT or AZURE_AI_PROJECT_ENDPOINT, or pass --endpoint <project-endpoint>.");

string deploymentName = FirstNonEmpty(
        options.DeploymentName,
        Environment.GetEnvironmentVariable("FOUNDRY_MODEL"),
        Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME"))
    ?? "gpt-5-mini";

Console.WriteLine("Procurement Quote Review Harness");
Console.WriteLine($"Foundry project: {endpoint}");
Console.WriteLine($"Model deployment: {deploymentName}");
Console.WriteLine($"Workspace: {workingDirectory}");
Console.WriteLine($"Output write approval: {(options.StrictApprovals ? "strict/manual" : "auto-approve output/* saves")}");
Console.WriteLine();

// DefaultAzureCredential is convenient for samples. For production, use the narrowest
// credential that matches your hosting environment, such as ManagedIdentityCredential.
AIProjectClient projectClient = new(
    new Uri(endpoint),
    new DefaultAzureCredential(),
    new AIProjectClientOptions { RetryPolicy = new ClientRetryPolicy(3) });

IChatClient chatClient = projectClient
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(deploymentName);

Func<FunctionCallContent, ValueTask<bool>>[] approvalRules = options.StrictApprovals
    ? [FileAccessProvider.ReadOnlyToolsAutoApprovalRule]
    : [FileAccessProvider.ReadOnlyToolsAutoApprovalRule, OutputOnlySaveAutoApprovalRule];

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    Name = "ProcurementHarness",
    Description = "Reviews vendor quotes against requirements and procurement policy.",
    MaxContextWindowTokens = options.MaxContextWindowTokens,
    MaxOutputTokens = options.MaxOutputTokens,
    FileAccessStore = new FileSystemAgentFileStore(workingDirectory),
    FileMemoryStore = new FileSystemAgentFileStore(Path.Combine(workingDirectory, ".agent-memory")),
    DisableWebSearch = true,
    DisableAgentSkillsProvider = true,
    LoopEvaluators =
    [
        new TodoCompletionLoopEvaluator(new TodoCompletionLoopEvaluatorOptions
        {
            Modes = ["execute"],
            FeedbackMessageTemplate =
                $"Continue the procurement review in execute mode. Remaining incomplete todos:{Environment.NewLine}{TodoCompletionLoopEvaluator.RemainingTodosPlaceholder}{Environment.NewLine}Read the required files, complete the remaining todos, and save output/recommendation.md before stopping.",
        }),
    ],
    LoopAgentOptions = new LoopAgentOptions
    {
        MaxIterations = options.MaxLoopIterations,
        ExcludeOnBehalfOfMessages = true,
    },
    MaximumIterationsPerRequest = options.MaximumIterationsPerRequest,
    ToolApprovalAgentOptions = new ToolApprovalAgentOptions
    {
        AutoApprovalRules = approvalRules,
    },
    ChatOptions = new ChatOptions
    {
        Instructions = SampleText.ProcurementInstructions,
        MaxOutputTokens = options.MaxOutputTokens,
    },
});

string prompt = options.Prompt ?? SampleText.DefaultPrompt;
AgentSession session = await agent.CreateSessionAsync();
bool approvalRequested = await RunAgentTurnAsync(agent, session, prompt, "Planning turn");

if (!approvalRequested && options.PlanOnly)
{
    Console.WriteLine();
    Console.WriteLine("Plan-only mode finished. Rerun without --plan-only to let the host set execute mode and complete the review.");
    return 0;
}

if (!approvalRequested)
{
    AgentModeProvider modeProvider = agent.GetService<AgentModeProvider>()
        ?? throw new InvalidOperationException("Agent mode provider is not available.");

    Console.WriteLine();
    Console.WriteLine($"Mode after planning: {modeProvider.GetMode(session)}");
    modeProvider.SetMode(session, "execute");
    Console.WriteLine("Host approval granted. Mode set to execute.");
    approvalRequested = await RunAgentTurnAsync(agent, session, SampleText.ExecuteApprovalPrompt, "Execution turn");
}

string recommendationPath = Path.Combine(workingDirectory, "output", "recommendation.md");
if (File.Exists(recommendationPath))
{
    Console.WriteLine();
    Console.WriteLine($"Recommendation written to: {recommendationPath}");
}
else if (!approvalRequested)
{
    Console.WriteLine();
    Console.WriteLine("No recommendation file was written. The run is incomplete because output/recommendation.md is the expected artifact.");
    return 1;
}

return approvalRequested ? 2 : 0;

static async Task<bool> RunAgentTurnAsync(AIAgent agent, AgentSession session, string prompt, string label)
{
    bool approvalRequested = false;

    Console.WriteLine(label);
    Console.WriteLine("User request:");
    Console.WriteLine(prompt);
    Console.WriteLine();
    Console.WriteLine("Agent output:");

    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, session))
    {
        foreach (AIContent content in update.Contents)
        {
            if (content is ToolApprovalRequestContent approvalRequest)
            {
                approvalRequested = true;
                string toolName = approvalRequest.ToolCall is FunctionCallContent functionCall
                    ? functionCall.Name
                    : "unknown tool";
                Console.WriteLine();
                Console.WriteLine($"Approval required for {toolName}. Rerun without --strict-approvals to auto-approve safe output writes.");
            }
        }

        Console.Write(update.Text);
    }

    Console.WriteLine();
    return approvalRequested;
}

static ValueTask<bool> OutputOnlySaveAutoApprovalRule(FunctionCallContent functionCall)
{
    if (!string.Equals(functionCall.Name, FileAccessProvider.SaveFileToolName, StringComparison.Ordinal))
    {
        return ValueTask.FromResult(false);
    }

    if (functionCall.Arguments is null ||
        !functionCall.Arguments.TryGetValue("fileName", out object? rawFileName) ||
        TryGetString(rawFileName) is not { } fileName)
    {
        return ValueTask.FromResult(false);
    }

    string normalized = fileName.Replace('\\', '/').TrimStart('/');
    bool isOutputFile = normalized.StartsWith("output/", StringComparison.OrdinalIgnoreCase);
    return ValueTask.FromResult(isOutputFile);
}

static string? TryGetString(object? value)
{
    return value switch
    {
        null => null,
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement element => element.ToString(),
        _ => value.ToString(),
    };
}

static WorkspaceCheck CheckWorkspace(string workingDirectory)
{
    string[] requiredFiles =
    [
        "requirements.md",
        "procurement-policy.md",
        "evaluation-rubric.md",
        "quotes/vendor-a.md",
        "quotes/vendor-b.md",
        "quotes/vendor-c.md",
    ];

    string[] missing = requiredFiles
        .Where(file => !File.Exists(Path.Combine(workingDirectory, file)))
        .ToArray();

    return new WorkspaceCheck(missing);
}

static void PrintWorkspaceSummary(string workingDirectory, WorkspaceCheck check)
{
    Console.WriteLine("Procurement Quote Review Harness check");
    Console.WriteLine($"Workspace: {workingDirectory}");
    Console.WriteLine($"Required files: {(check.IsValid ? "ok" : "missing")}");
    Console.WriteLine();
    Console.WriteLine("Seed files:");

    foreach (string relativePath in Directory.EnumerateFiles(workingDirectory, "*", SearchOption.AllDirectories)
                 .Select(file => Path.GetRelativePath(workingDirectory, file))
                 .Where(relativePath => !relativePath.StartsWith($".agent-memory{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                 .Where(relativePath => !relativePath.StartsWith($"output{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                 .Where(relativePath => Path.GetFileName(relativePath) is not ".gitignore" and not ".gitkeep")
                 .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine($"  - {relativePath}");
    }
}

static void PrintConfigurationSummary(SampleOptions options)
{
    string? endpoint = FirstNonEmpty(
        options.Endpoint,
        Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT"),
        Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT"));

    string? deployment = FirstNonEmpty(
        options.DeploymentName,
        Environment.GetEnvironmentVariable("FOUNDRY_MODEL"),
        Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME"));

    Console.WriteLine();
    Console.WriteLine("Foundry configuration:");
    Console.WriteLine($"  Endpoint: {(endpoint is null ? "not set" : "set")}");
    Console.WriteLine($"  Deployment: {deployment ?? "gpt-5-mini (default)"}");
    Console.WriteLine();
    Console.WriteLine("Run:");
    Console.WriteLine("  dotnet run --project .\\backend\\ProcurementHarness.Console\\ProcurementHarness.Console.csproj");
}

static string ResolveSampleRoot(string? configuredWorkingDirectory)
{
    if (!string.IsNullOrWhiteSpace(configuredWorkingDirectory))
    {
        return Directory.GetCurrentDirectory();
    }

    string current = Directory.GetCurrentDirectory();
    string? cursor = current;

    while (cursor is not null)
    {
        if (Directory.Exists(Path.Combine(cursor, "working")) &&
            File.Exists(Path.Combine(cursor, "README.md")) &&
            Directory.Exists(Path.Combine(cursor, "backend")))
        {
            return cursor;
        }

        cursor = Directory.GetParent(cursor)?.FullName;
    }

    // dotnet run may execute from a caller-selected directory. The content files are
    // also copied beside the app so the sample still works from the build output.
    string outputRoot = AppContext.BaseDirectory;
    if (Directory.Exists(Path.Combine(outputRoot, "working")))
    {
        return outputRoot;
    }

    return current;
}

static string? FirstNonEmpty(params string?[] values)
{
    return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        Procurement Quote Review Harness

        Usage:
          dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj -- [options]

        Options:
          --check                    Validate the seeded workspace without calling a model.
          --endpoint <url>            Microsoft Foundry project endpoint.
          --deployment <name>         Foundry model deployment name. Defaults to gpt-5-mini.
          --working-dir <path>        Override the file-access working directory.
          --prompt <text>             Override the default procurement review prompt.
          --plan-only                 Stop after the planning turn.
          --strict-approvals          Auto-approve read-only file access only; output writes require approval.
          --help                      Show this help.
        """);
}

internal sealed record WorkspaceCheck(IReadOnlyList<string> MissingFiles)
{
    public bool IsValid => this.MissingFiles.Count == 0;
}

internal sealed record SampleOptions(
    bool CheckOnly,
    bool ShowHelp,
    bool PlanOnly,
    bool StrictApprovals,
    string? Endpoint,
    string? DeploymentName,
    string? WorkingDirectory,
    string? Prompt,
    int MaxContextWindowTokens = 1_050_000,
    int MaxOutputTokens = 16_000,
    int MaxLoopIterations = 4,
    int MaximumIterationsPerRequest = 80)
{
    public static SampleOptions Parse(string[] args)
    {
        bool checkOnly = false;
        bool showHelp = false;
        bool planOnly = false;
        bool strictApprovals = false;
        string? endpoint = null;
        string? deployment = null;
        string? workingDirectory = null;
        string? prompt = null;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--check":
                    checkOnly = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                case "--strict-approvals":
                    strictApprovals = true;
                    break;
                case "--plan-only":
                    planOnly = true;
                    break;
                case "--endpoint":
                    endpoint = ReadValue(args, ref index, arg);
                    break;
                case "--deployment":
                    deployment = ReadValue(args, ref index, arg);
                    break;
                case "--working-dir":
                    workingDirectory = ReadValue(args, ref index, arg);
                    break;
                case "--prompt":
                    prompt = ReadValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}. Use --help for options.");
            }
        }

        return new SampleOptions(
            checkOnly,
            showHelp,
            planOnly,
            strictApprovals,
            endpoint,
            deployment,
            workingDirectory,
            prompt);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        return args[++index];
    }
}

internal static class SampleText
{
    public const string DefaultPrompt =
        """
        Review the procurement workspace and recommend one vendor for the managed laptop refresh.

        Use the harness deliberately:
        1. Create todos for the review.
        2. List and read the requirement, policy, rubric, and quote files.
        3. Compare each vendor against the weighted rubric.
        4. Call out policy risks and negotiation points.
        5. Save the final recommendation to output/recommendation.md.
        """;

    public const string ExecuteApprovalPrompt =
        """
        The procurement manager approves the plan. Continue in execute mode.

        Complete the review now:
        - read the requirements, policy, rubric, and every vendor quote
        - use the weighted rubric to choose one vendor
        - complete all review todos
        - save the final report to output/recommendation.md

        After saving the file, respond with a one-sentence completion summary.
        """;

    public const string ProcurementInstructions =
        """
        You are a procurement operations analyst.

        You have file_access_* tools scoped to the sample working directory. Treat that folder as the only source of quote, policy, and requirement truth.

        Work style:
        - Use the todo tools before doing the review.
        - Use plan mode to define the review steps, then execute mode to perform them.
        - Read the requirements, policy, rubric, and each vendor quote before deciding.
        - Use evidence from the files. Do not invent vendor capabilities or prices.
        - Keep all calculations transparent enough for a procurement manager to audit.
        - Prefer a concise recommendation with a decision table, risks, and next actions.

        File rules:
        - Do not modify source input files.
        - Write generated artifacts only under output/.
        - Save the final report as output/recommendation.md unless the user asks for a different output path.
        - Do not delete files.
        """;
}
