# MAF Procurement Harness

A console-first sample demonstrating **Agent Harness** in **Microsoft Agent Framework** with a Foundry-backed model, controlled file access, todo tracking, plan/execute mode management, and approval-aware output writes.

> **Blog post:** [Agent Harness: The Missing Middle Between Model Reasoning and Real Execution](https://arafattehsin.com/blog/agent-harness-missing-middle/)

## Scenario

**Managed Laptop Refresh Vendor Review** - a procurement analyst needs to compare three vendor quotes against requirements, policy, and a weighted rubric. The harness gives the agent a bounded runtime shape:

```text
Procurement request
        |
        v
Agent Harness session
        |
        +--> plan mode: inspect workspace, create todos, prepare review plan
        |
        +--> host approval: switch session to execute mode
        |
        +--> execute mode:
             +--> read requirements, policy, rubric, and quotes
             +--> score vendors with auditable evidence
             +--> write output/recommendation.md
             +--> close remaining todos
```

The sample intentionally keeps the business data local and inspectable. The agent reads from `working/`, writes only under `working/output/`, and produces a procurement recommendation that can be reviewed on disk.

## Stack

- **Runtime:** .NET 8 console app
- **AI platform:** Microsoft Foundry project model deployment
- **Agent SDK:** Microsoft Agent Framework preview packages
- **Harness features:** `HarnessAgent`, `TodoProvider`, `AgentModeProvider`, `FileAccessProvider`, `FileMemoryProvider`, `ToolApprovalAgent`, `TodoCompletionLoopEvaluator`

## Project Layout

```text
maf-procurement-harness/
├── README.md
├── backend/
│   ├── ProcurementHarness.sln
│   └── ProcurementHarness.Console/
│       ├── Program.cs
│       └── ProcurementHarness.Console.csproj
└── working/
    ├── requirements.md
    ├── procurement-policy.md
    ├── evaluation-rubric.md
    ├── quotes/
    │   ├── vendor-a.md
    │   ├── vendor-b.md
    │   └── vendor-c.md
    └── output/
        └── .gitkeep
```

## What It Demonstrates

| Capability | How the sample uses it |
| --- | --- |
| Foundry-backed chat client | Builds an `IChatClient` from `AIProjectClient`, then wraps it as a harness agent. |
| Todo tracking | The agent creates and closes explicit review todos instead of relying on hidden reasoning. |
| Plan/execute modes | The first turn plans. The host app then approves the plan by switching the session to `execute`. |
| Controlled file access | File tools are scoped to the sample `working/` directory. |
| Safe output writes | Read-only file tools are auto-approved; `file_access_save_file` is auto-approved only under `output/`. |
| File-backed memory | The agent can persist run notes under `.agent-memory/`. |
| Bounded execution loop | `TodoCompletionLoopEvaluator` can re-invoke the agent while execute-mode todos remain open. |

## Prerequisites

- .NET 8 SDK or later
- Azure CLI login with access to a Microsoft Foundry project
- A deployed model in that project

The sample restores Microsoft Agent Framework packages from NuGet, including:

- `Microsoft.Agents.AI.Foundry`
- `Microsoft.Agents.AI.Harness`
- `Azure.Identity`

## Configuration

Set your Foundry project endpoint and model deployment:

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://<your-project>.services.ai.azure.com/api/projects/<your-project-name>"
$env:FOUNDRY_MODEL = "<your-model-deployment-name>"
```

The sample also accepts these environment variable aliases:

| Setting | Purpose |
| --- | --- |
| `FOUNDRY_PROJECT_ENDPOINT` or `AZURE_AI_PROJECT_ENDPOINT` | Microsoft Foundry project endpoint |
| `FOUNDRY_MODEL` or `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Foundry model deployment name |

You can also pass the endpoint and deployment as command-line options:

```powershell
dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj -- --endpoint "https://<project>.services.ai.azure.com/api/projects/<project-name>" --deployment "<deployment-name>"
```

## Validate Without Calling a Model

```powershell
cd C:\Users\AT\source\repos\generative-ai\samples\dotnet\maf-procurement-harness
dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj -- --check
```

## Run

Build first:

```powershell
dotnet build .\backend\ProcurementHarness.sln
```

Run the default procurement review:

```powershell
dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj
```

Expected output artifact:

```text
working/output/recommendation.md
```

## Run Options

Stop after planning and inspect the mode boundary:

```powershell
dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj -- --plan-only
```

Surface write approvals instead of auto-approving safe output writes:

```powershell
dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj -- --strict-approvals
```

Override the business request:

```powershell
dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj -- --prompt "Compare the quotes and produce a negotiation brief for the top two vendors."
```

Use a different working directory:

```powershell
dotnet run --project .\backend\ProcurementHarness.Console\ProcurementHarness.Console.csproj -- --working-dir "C:\temp\procurement-workspace"
```

## Notes

- Generated run memory is written under `working/.agent-memory/` and ignored by git.
- Generated reports are written under `working/output/` and ignored by git except for `.gitkeep`.
- The sample does not auto-approve delete operations.
- The default run is deliberately two-phase so the host application owns the transition from planning to execution.
- The recommendation file is required; if the run finishes without `working/output/recommendation.md`, the app returns a non-zero exit code.
