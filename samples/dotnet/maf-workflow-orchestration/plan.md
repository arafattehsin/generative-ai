# PolicyPack Builder - Implementation Plan

> **Demo App**: A production-quality UI-driven workflow showcasing Microsoft Agent Framework's Sequential orchestration pattern with Microsoft Foundry (Azure AI Foundry) backend.

## Overview

**PolicyPack Builder** transforms messy draft policies/customer communications into structured, compliant output packages through a 6-step sequential workflow pipeline with live progress visualization.

### Key Features

- 6-step sequential workflow with mixed LLM and non-LLM processing
- Real-time step-by-step progress via SignalR
- PII detection and redaction before LLM processing
- Compliance checking with configurable rules
- Re-run from any completed step (SQLite-persisted outputs)
- HTML export of final packages
- Modern React UI with Mantine components

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PolicyPack Builder                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         Frontend (React + Vite)                      │   │
│  │  ┌──────────┐  ┌──────────────┐  ┌────────────────────────────────┐ │   │
│  │  │  Home    │  │  Runs List   │  │       Run Details              │ │   │
│  │  │  Page    │  │    Page      │  │  (Stepper + Step Inspector)    │ │   │
│  │  └────┬─────┘  └──────┬───────┘  └───────────────┬────────────────┘ │   │
│  │       │               │                          │                   │   │
│  │       └───────────────┴──────────────────────────┘                   │   │
│  │                        │                                              │   │
│  │              TanStack Query + SignalR Client                         │   │
│  └──────────────────────────┬───────────────────────────────────────────┘   │
│                             │ HTTP + WebSocket                               │
│  ┌──────────────────────────┴───────────────────────────────────────────┐   │
│  │                      Backend (ASP.NET Core)                           │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐   │   │
│  │  │  REST API   │  │  SignalR    │  │    WorkflowOrchestrator     │   │   │
│  │  │  Endpoints  │  │  RunsHub    │  │    (MAF Sequential)         │   │   │
│  │  └──────┬──────┘  └──────┬──────┘  └──────────────┬──────────────┘   │   │
│  │         │                │                        │                   │   │
│  │         └────────────────┼────────────────────────┘                   │   │
│  │                          │                                            │   │
│  │  ┌───────────────────────┴────────────────────────────────────────┐  │   │
│  │  │                    Application Services                         │  │   │
│  │  │  ┌────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │  │   │
│  │  │  │ ILlmClient │  │ PiiRedactor │  │ ComplianceRulesEngine   │  │  │   │
│  │  │  └────────────┘  └─────────────┘  └─────────────────────────┘  │  │   │
│  │  └────────────────────────────────────────────────────────────────┘  │   │
│  │                          │                                            │   │
│  │  ┌───────────────────────┴────────────────────────────────────────┐  │   │
│  │  │                    Infrastructure                               │  │   │
│  │  │  ┌────────────────┐  ┌─────────────────────────────────────┐   │  │   │
│  │  │  │ SQLite + EF    │  │ Azure OpenAI (ChatClientAgent)      │   │  │   │
│  │  │  │ Core           │  │ via DefaultAzureCredential          │   │  │   │
│  │  │  └────────────────┘  └─────────────────────────────────────┘   │  │   │
│  │  └────────────────────────────────────────────────────────────────┘  │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Workflow Pipeline

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  1. Intake +    │───▶│  2. Extract     │───▶│  3. Draft       │
│  Normalize      │    │  Facts (LLM)    │    │  Summary (LLM)  │
│  (non-LLM)      │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                      │
┌─────────────────┐    ┌─────────────────┐    ┌───────▼─────────┐
│  6. Final       │◀───│  5. Brand Tone  │◀───│  4. Compliance  │
│  Package (HTML) │    │  Rewrite (LLM)  │    │  Check (LLM+    │
│  (non-LLM)      │    │                 │    │  Rules)         │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Step Details

| #   | Step Name                  | Type        | Description                                                                        |
| --- | -------------------------- | ----------- | ---------------------------------------------------------------------------------- |
| 1   | **Intake + Normalize**     | Non-LLM     | Whitespace cleanup, encoding normalization, basic text sanitization                |
| 2   | **Extract Facts**          | LLM         | Extract entities, key points, risks, required disclaimers → strict JSON output     |
| 3   | **Draft Customer Summary** | LLM         | Generate concise, plain-English summary suitable for customers                     |
| 4   | **Compliance Check + Fix** | LLM + Rules | Check restricted phrases, inject disclaimers, output JSON with issues + fixed text |
| 5   | **Brand Tone Rewrite**     | LLM         | Rewrite for target tone while preserving meaning and disclaimers                   |
| 6   | **Final Package**          | Non-LLM     | Format as structured HTML with sections, styling, metadata                         |

---

## Technology Stack

### Backend

- **.NET 9** with ASP.NET Core Web API
- **Microsoft Agent Framework Workflows** - Sequential pattern with `WorkflowBuilder.AddEdge()`
- **SignalR** - Real-time progress streaming
- **SQLite + EF Core** - Run and step persistence
- **Azure OpenAI** via `ChatClientAgent` + `DefaultAzureCredential`
- **Clean Architecture** - Domain / Application / Infrastructure / Api layers

### Frontend

- **React 18** + **TypeScript**
- **Vite** - Fast development server and build
- **Mantine v7** - UI component library (light mode)
- **React Router v6** - Client-side routing
- **TanStack Query v5** - Server state management
- **@microsoft/signalr** - Real-time connection

---

## Folder Structure

```
maf-workflow-orchestration/
├── plan.md                          # This file
├── README.md                        # Setup and run instructions
│
├── backend/
│   ├── PolicyPackBuilder.sln
│   │
│   ├── PolicyPackBuilder.Domain/
│   │   ├── Entities/
│   │   │   ├── WorkflowRun.cs
│   │   │   └── WorkflowStepRun.cs
│   │   ├── Enums/
│   │   │   ├── WorkflowStatus.cs
│   │   │   └── StepStatus.cs
│   │   └── ValueObjects/
│   │       ├── WorkflowContext.cs
│   │       └── WorkflowOptions.cs
│   │
│   ├── PolicyPackBuilder.Application/
│   │   ├── Interfaces/
│   │   │   ├── ILlmClient.cs
│   │   │   ├── IWorkflowRunRepository.cs
│   │   │   ├── IWorkflowStepRunRepository.cs
│   │   │   ├── IPiiRedactionService.cs
│   │   │   └── IComplianceRulesEngine.cs
│   │   ├── Services/
│   │   │   ├── WorkflowOrchestrator.cs
│   │   │   ├── PiiRedactionService.cs
│   │   │   └── ComplianceRulesEngine.cs
│   │   ├── Executors/
│   │   │   ├── IntakeNormalizeExecutor.cs
│   │   │   ├── ExtractFactsExecutor.cs
│   │   │   ├── DraftSummaryExecutor.cs
│   │   │   ├── ComplianceCheckExecutor.cs
│   │   │   ├── BrandToneRewriteExecutor.cs
│   │   │   └── FinalPackageExecutor.cs
│   │   ├── DTOs/
│   │   │   ├── CreateRunRequest.cs
│   │   │   ├── RunResponse.cs
│   │   │   ├── StepResponse.cs
│   │   │   └── SampleResponse.cs
│   │   └── Utilities/
│   │       └── SnapshotTruncator.cs
│   │
│   ├── PolicyPackBuilder.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── PolicyPackDbContext.cs
│   │   │   ├── WorkflowRunRepository.cs
│   │   │   └── WorkflowStepRunRepository.cs
│   │   ├── LLM/
│   │   │   └── ChatClientLlmClient.cs
│   │   └── DependencyInjection.cs
│   │
│   └── PolicyPackBuilder.Api/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Controllers/
│       │   ├── RunsController.cs
│       │   └── SamplesController.cs
│       ├── Hubs/
│       │   └── RunsHub.cs
│       └── BackgroundServices/
│           └── WorkflowHostedService.cs
│
└── frontend/
    ├── package.json
    ├── vite.config.ts
    ├── tsconfig.json
    ├── index.html
    │
    └── src/
        ├── main.tsx
        ├── App.tsx
        ├── api/
        │   ├── client.ts
        │   ├── runs.ts
        │   └── samples.ts
        ├── hooks/
        │   ├── useSignalR.ts
        │   └── useRuns.ts
        ├── pages/
        │   ├── HomePage.tsx
        │   ├── RunsPage.tsx
        │   └── RunDetailsPage.tsx
        ├── components/
        │   ├── Layout.tsx
        │   ├── StepTimeline.tsx
        │   ├── StepInspector.tsx
        │   ├── OptionsPanel.tsx
        │   ├── SampleSelector.tsx
        │   └── ConnectionStatus.tsx
        └── types/
            └── index.ts
```

---

## Implementation Steps

### Phase 1: Backend Scaffolding (Steps 1-3)

#### Step 1: Create Solution and Projects

```powershell
# Create solution structure
cd maf-workflow-orchestration
mkdir backend
cd backend
dotnet new sln -n PolicyPackBuilder
dotnet new classlib -n PolicyPackBuilder.Domain -f net9.0
dotnet new classlib -n PolicyPackBuilder.Application -f net9.0
dotnet new classlib -n PolicyPackBuilder.Infrastructure -f net9.0
dotnet new webapi -n PolicyPackBuilder.Api -f net9.0
# Add project references
dotnet sln add PolicyPackBuilder.Domain
dotnet sln add PolicyPackBuilder.Application
dotnet sln add PolicyPackBuilder.Infrastructure
dotnet sln add PolicyPackBuilder.Api
```

#### Step 2: Implement Domain Layer

**Entities:**

- `WorkflowRun`: Id (Guid), ParentRunId (Guid?), RootRunId (Guid?), CreatedAt, Status, OptionsJson, InputTextOriginal, InputTextRedacted, FinalOutputHtml, TotalDurationMs, Error
- `WorkflowStepRun`: Id, RunId, StepName, StepOrder, Status, StartedAt, CompletedAt, DurationMs, InputSnapshot, InputIsTruncated, InputFullLength, OutputSnapshot, OutputIsTruncated, OutputFullLength, WarningsJson, Error

**Enums:**

- `WorkflowStatus`: Pending, Running, Completed, Failed, Canceled
- `StepStatus`: Pending, Running, Completed, Failed, Skipped

**Value Objects:**

- `WorkflowContext`: Carries data between steps
- `WorkflowOptions`: audience, tone, strictCompliance

#### Step 3: Implement Application Layer

**Interfaces:**

- `ILlmClient`: InvokeAsync(prompt, systemMessage, ct)
- `IWorkflowRunRepository`: CRUD + GetWithStepsAsync
- `IWorkflowStepRunRepository`: GetCompletedStepsUpToAsync
- `IPiiRedactionService`: Redact(text) → RedactionResult
- `IComplianceRulesEngine`: Check(text, audience, strictMode)

**Utilities:**

- `SnapshotTruncator`: Truncate(text, maxLength=50000) → returns (truncatedText, isTruncated, fullLength)

---

### Phase 2: Workflow Executors (Steps 4-5)

#### Step 4: Build 6 MAF Executors

Each executor extends `Executor<WorkflowContext, WorkflowContext>`:

1. **IntakeNormalizeExecutor** (non-LLM)
   - Normalize whitespace, remove control characters
   - Validate minimum text length

2. **ExtractFactsExecutor** (LLM)
   - Prompt: Output strict JSON schema

   ```json
   {
     "entities": [{ "type": "", "value": "" }],
     "key_points": ["..."],
     "risks": ["..."],
     "required_disclaimers": ["..."]
   }
   ```

3. **DraftSummaryExecutor** (LLM)
   - Concise, plain English, customer-suitable
   - Reference extracted facts

4. **ComplianceCheckExecutor** (LLM + Rules)
   - LLM outputs:

   ```json
   {
     "issues": [{ "type": "", "detail": "", "severity": "low|med|high" }],
     "fixed_text": "..."
   }
   ```

   - Non-LLM rules check restricted phrases, inject required disclaimers

5. **BrandToneRewriteExecutor** (LLM)
   - Rewrite for target tone (Professional/Friendly/Formal)
   - Preserve meaning + all disclaimers

6. **FinalPackageExecutor** (non-LLM)
   - Generate structured HTML with sections
   - Include metadata, timestamp, options used

#### Step 5: Create WorkflowOrchestrator Service

- Constructs MAF workflow: `WorkflowBuilder.AddEdge()` for sequential wiring
- Executes via `InProcessExecution.StreamAsync()`
- Persists step snapshots after each `ExecutorCompletedEvent`
- Emits SignalR events: `StepStarted`, `StepCompleted`, `StepFailed`, `RunCompleted`
- Supports `CancellationToken` for cancellation
- **Re-run mechanism**: Load completed steps from parent run, reconstruct context, execute remaining steps

---

### Phase 3: Infrastructure & API (Steps 6-8)

#### Step 6: Implement Infrastructure Layer

**Persistence:**

- `PolicyPackDbContext`: EF Core with SQLite
- `WorkflowRunRepository`: Full CRUD + includes steps
- `WorkflowStepRunRepository`: GetCompletedStepsUpToAsync for re-run support

**LLM Client:**

- `ChatClientLlmClient`: Wraps `ChatClientAgent` + `AzureOpenAIClient`
- Uses `DefaultAzureCredential` for auth
- Max output length safety: re-invoke if >2000 chars

```csharp
// TODO: Upgrade to PersistentAgentsClient for multi-session workflows
// See: Agents/FoundryAgent sample for pattern
```

#### Step 7: Build SignalR Hub

`RunsHub` at `/hubs/runs`:

- `JoinRun(runId)`: Client joins group by runId
- Server broadcasts:
  - `StepStarted(runId, stepName, timestamp)`
  - `StepCompleted(runId, stepName, durationMs, previewText)`
  - `StepFailed(runId, stepName, error)`
  - `RunCompleted(runId, totalDurationMs, success)`

#### Step 8: Build API Endpoints

| Method | Endpoint                               | Description                        |
| ------ | -------------------------------------- | ---------------------------------- |
| POST   | `/api/runs`                            | Create and start new run           |
| GET    | `/api/runs`                            | List all runs (paginated)          |
| GET    | `/api/runs/{id}`                       | Get run details with steps         |
| POST   | `/api/runs/{id}/cancel`                | Cancel running workflow            |
| POST   | `/api/runs/{id}/rerun?fromStep={name}` | Re-run from step (creates new run) |
| GET    | `/api/runs/{id}/export`                | Download final HTML                |
| GET    | `/api/samples`                         | Get sample policy drafts           |

---

### Phase 4: Frontend (Steps 9-13)

#### Step 9: Scaffold React + Vite App

```powershell
cd maf-workflow-orchestration
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install @mantine/core @mantine/hooks @mantine/notifications @emotion/react
npm install @tanstack/react-query react-router-dom @microsoft/signalr
npm install @tabler/icons-react
```

#### Step 10: Build Home Page

Components:

- `Textarea` with autosize for input
- `Select` dropdown for samples (2 seeded: HR Policy, Product Warranty)
- `SegmentedControl` for audience: Customer / Internal / Legal
- `Select` for tone: Professional / Friendly / Formal
- `Switch` for strict compliance mode
- `Button` with loading state for Run
- Sidebar showing 6 workflow steps with icons and descriptions

#### Step 11: Create Run Details Page

Components:

- `Stepper` (vertical) showing step progression
- Status icons: pending (gray), running (blue spinner), completed (green check), failed (red x)
- Connection status badge (connected/disconnected)
- Step click expands inspector panel
- `StepInspector`: Input/Output with `Code` component (syntax highlight, copy, collapse)
- "Re-run from here" button on completed steps
- "Export HTML" button when completed

#### Step 12: Implement SignalR Hook

`useSignalR(runId)`:

- Connect to `/hubs/runs`
- Call `JoinRun(runId)` on mount
- Listen for step events
- Invalidate TanStack Query on events
- Show toast notifications via Mantine

#### Step 13: Build Runs List Page

- Mantine `Table` with columns:
  - Created (formatted date)
  - Status (badge with color)
  - Duration
  - Audience
  - Tone
  - Lineage indicator (🔁 if re-run)
- Row click → navigate to details
- Empty state message

---

### Phase 5: Polish & Documentation (Steps 14-15)

#### Step 14: Add Sample Policy Drafts

Two seeded samples for `GET /api/samples`:

1. **HR Policy Draft** - Remote work policy with informal language, missing disclaimers
2. **Product Warranty Draft** - Warranty terms with unclear language, potential compliance issues

#### Step 15: Write README

Include:

- Prerequisites (Node 18+, .NET 9, Azure CLI)
- Environment variables setup
- Run commands (backend + frontend)
- CORS configuration notes
- Auth troubleshooting (Azure CLI login)
- **Note**: "This demo uses SQLite-persisted step outputs for re-run. Official MAF CheckpointManager for durable checkpointing will be introduced in a later post."

---

## Data Models

### WorkflowRun

```csharp
public sealed class WorkflowRun
{
    public Guid Id { get; set; }
    public Guid? ParentRunId { get; set; }      // For re-run lineage
    public Guid? RootRunId { get; set; }        // Points to original run for grouping
    public DateTime CreatedAt { get; set; }
    public WorkflowStatus Status { get; set; }
    public string OptionsJson { get; set; }      // Serialized WorkflowOptions
    public string InputTextOriginal { get; set; }
    public string InputTextRedacted { get; set; }
    public string? FinalOutputHtml { get; set; }
    public long? TotalDurationMs { get; set; }
    public string? Error { get; set; }

    // Navigation
    public WorkflowRun? ParentRun { get; set; }
    public ICollection<WorkflowStepRun> Steps { get; set; }
}
```

### WorkflowStepRun

```csharp
public sealed class WorkflowStepRun
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string StepName { get; set; }
    public int StepOrder { get; set; }
    public StepStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? InputSnapshot { get; set; }
    public bool InputIsTruncated { get; set; }
    public int? InputFullLength { get; set; }
    public string? OutputSnapshot { get; set; }
    public bool OutputIsTruncated { get; set; }
    public int? OutputFullLength { get; set; }
    public string? WarningsJson { get; set; }
    public string? Error { get; set; }

    // Navigation
    public WorkflowRun Run { get; set; }
}
```

### WorkflowContext

```csharp
public sealed class WorkflowContext
{
    public string NormalizedInput { get; set; }
    public string? ExtractedFactsJson { get; set; }
    public string? DraftCustomerSummary { get; set; }
    public string? ComplianceIssuesJson { get; set; }
    public string? CompliantText { get; set; }
    public string? ToneRewrittenText { get; set; }
    public string? FinalHtml { get; set; }
    public List<string> Warnings { get; set; } = [];
    public WorkflowOptions Options { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this);
    public static WorkflowContext FromJson(string json) =>
        JsonSerializer.Deserialize<WorkflowContext>(json);
}
```

---

## LLM Prompts

### Extract Facts

```
You are a document analysis expert. Extract structured information from the policy/communication text.

Output STRICT JSON only with this exact schema:
{
  "entities": [{"type": "person|organization|date|amount|product", "value": "..."}],
  "key_points": ["Main point 1", "Main point 2"],
  "risks": ["Potential risk or unclear area"],
  "required_disclaimers": ["Any legally required disclaimers based on content type"]
}

Do not include any text outside the JSON object.
```

### Draft Customer Summary

```
You are a professional communications writer. Create a concise, clear summary of this policy/communication for customers.

Requirements:
- Use plain English, avoid jargon
- Be concise (max 300 words)
- Highlight key takeaways
- Maintain accuracy to source material
- Suitable for {audience} audience
```

### Compliance Check

```
You are a compliance reviewer. Analyze the text for compliance issues and fix them.

Check for:
- Restricted phrases that could be misleading
- Missing required disclaimers
- Unclear or ambiguous language
- Potential legal risks

Output STRICT JSON only:
{
  "issues": [
    {"type": "restricted_phrase|missing_disclaimer|ambiguous|legal_risk", "detail": "Description", "severity": "low|med|high"}
  ],
  "fixed_text": "The corrected text with all issues addressed"
}

{strictModeInstruction}
```

### Brand Tone Rewrite

```
You are a brand voice specialist. Rewrite the text to match the {tone} tone.

Tone guidelines:
- Professional: Formal, authoritative, business-appropriate
- Friendly: Warm, approachable, conversational but respectful
- Formal: Highly structured, official, ceremonial

CRITICAL: You MUST preserve:
1. All factual content and meaning
2. All disclaimers exactly as written
3. All legal/compliance language

Only adjust the style and word choice around the protected content.
```

---

## SignalR Events

### Event Payloads

```typescript
interface StepStartedEvent {
  runId: string;
  stepName: string;
  stepOrder: number;
  timestamp: string;
}

interface StepCompletedEvent {
  runId: string;
  stepName: string;
  stepOrder: number;
  durationMs: number;
  previewText?: string; // First 100 chars of output
  timestamp: string;
}

interface StepFailedEvent {
  runId: string;
  stepName: string;
  stepOrder: number;
  error: string;
  timestamp: string;
}

interface RunCompletedEvent {
  runId: string;
  success: boolean;
  totalDurationMs: number;
  timestamp: string;
}
```

---

## API Contracts

### POST /api/runs

**Request:**

```json
{
  "inputText": "Draft policy text...",
  "options": {
    "audience": "customer",
    "tone": "professional",
    "strictCompliance": true
  }
}
```

**Response:**

```json
{
  "runId": "guid",
  "status": "pending"
}
```

### GET /api/runs/{id}

**Response:**

```json
{
  "id": "guid",
  "parentRunId": null,
  "rootRunId": null,
  "createdAt": "2026-01-28T10:00:00Z",
  "status": "completed",
  "options": {
    "audience": "customer",
    "tone": "professional",
    "strictCompliance": true
  },
  "inputTextRedacted": "...",
  "finalOutputHtml": "<html>...</html>",
  "totalDurationMs": 12500,
  "steps": [
    {
      "id": "guid",
      "stepName": "IntakeNormalize",
      "stepOrder": 1,
      "status": "completed",
      "startedAt": "...",
      "completedAt": "...",
      "durationMs": 50,
      "inputSnapshot": "...",
      "outputSnapshot": "...",
      "outputIsTruncated": false
    }
  ]
}
```

### POST /api/runs/{id}/rerun?fromStep=ComplianceCheck

**Response:**

```json
{
  "runId": "new-guid",
  "parentRunId": "original-guid",
  "rootRunId": "original-guid",
  "status": "pending"
}
```

---

## Configuration

### Environment Variables

| Variable                         | Description                                  | Required |
| -------------------------------- | -------------------------------------------- | -------- |
| `AZURE_AI_PROJECT_ENDPOINT`      | Azure AI Foundry project endpoint            | Yes      |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Model deployment name (default: gpt-4o-mini) | No       |

### appsettings.json

```json
{
  "Workflow": {
    "MaxSnapshotLength": 50000,
    "MaxLlmOutputLength": 2000,
    "LlmRetryOnOverLength": true
  },
  "Compliance": {
    "RestrictedPhrases": ["guaranteed", "100% safe", "no risk"],
    "RequiredDisclaimers": {
      "customer": ["Terms and conditions apply."],
      "legal": ["This does not constitute legal advice."]
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=policypack.db"
  }
}
```

---

## Important Notes

### Re-Run Mechanism

> ⚠️ **Note**: This demo uses SQLite-persisted step outputs for re-run capability. This is NOT durable checkpointing. The official MAF `CheckpointManager` for durable checkpointing will be introduced in a later post in this series.

The re-run mechanism works as follows:

1. User clicks "Re-run from step N" on a completed run
2. API creates a NEW `WorkflowRun` with `ParentRunId` pointing to original run
3. System loads completed step outputs from parent run up to step N-1
4. Reconstructs `WorkflowContext` from those outputs
5. Executes steps N through end sequentially
6. Persists new step records (does NOT modify original run)

### LLM Client Upgrade Path

```csharp
// Current: ChatClientAgent with AzureOpenAIClient
// TODO: Upgrade to PersistentAgentsClient for multi-session workflows
// This will be covered in a future post when we discuss long-running agents
```

### Snapshot Truncation

For v1, we enforce a configurable max snapshot size (default 50,000 chars):

- If exceeded, truncate and append `"...[TRUNCATED]"` marker
- Set `OutputIsTruncated = true` and `OutputFullLength = actualLength`
- `// TODO v2: Implement artifact blob storage for full outputs when truncated`

---

## Success Criteria

- [ ] User can paste input text or select a sample
- [ ] User can configure audience, tone, and strict compliance mode
- [ ] User can start workflow and see real-time step progress
- [ ] Each step shows duration and status in timeline
- [ ] User can expand any step to see input/output snapshots
- [ ] User can re-run from any completed step (creates new run)
- [ ] User can export final HTML result
- [ ] User can view history of all runs
- [ ] Re-runs show lineage indicator in runs list
- [ ] SignalR connection status is visible
- [ ] Errors are handled gracefully with user-friendly messages
- [ ] App runs locally with only Azure AI Foundry as external dependency
