# MAF Concurrent Orchestration (OnboardFlow)

A full-stack sample demonstrating **concurrent agent orchestration** using **Microsoft Agent Framework** (MAF) with a fan-out/fan-in pattern for enterprise customer onboarding.

> **Blog post:** [Agent Orchestration Patterns — Part 3](http://arafattehsin.com/blog/agent-orchestration-patterns-part-3/)

## Scenario

**Enterprise SaaS Customer Onboarding** — an applicant request flows through sequential preparation stages, fans out to three concurrent expert reviewers, barrier-synchronizes, and reconverges into a final decision package.

```
IntakeNormalize → ExtractProfile → ┌─ SecurityReview  ─┐
                                    ├─ ComplianceReview ─┤ → AggregateFindings → CustomerNextSteps → FinalPackage
                                    └─ FinanceReview   ─┘
```

The concurrent review stage is built with MAF's `WorkflowBuilder`, `AddFanOutEdge`, and `AddFanInBarrierEdge` primitives — three `ChatClientAgent` reviewers run in parallel and synchronize through a barrier before aggregation.

## Stack

- **Backend:** ASP.NET Core (.NET 10), Microsoft Agent Framework, Azure OpenAI, EF Core + SQLite, SignalR
- **Frontend:** React 19 + TypeScript, Vite, Mantine UI v8, TanStack React Query, SignalR client

## Project Layout

```text
maf-concurrent-orchestration/
├── README.md
├── backend/
│   ├── OnboardFlow.slnx
│   ├── OnboardFlow.Api/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Properties/launchSettings.json
│   │   ├── Controllers/
│   │   │   ├── RunsController.cs
│   │   │   └── SamplesController.cs
│   │   └── Hubs/
│   │       └── RunsHub.cs
│   ├── OnboardFlow.Application/
│   │   ├── Executors/
│   │   │   ├── IntakeNormalizeExecutor.cs
│   │   │   ├── ExtractProfileExecutor.cs
│   │   │   ├── CustomerNextStepsExecutor.cs
│   │   │   ├── FinalPackageExecutor.cs
│   │   │   ├── WorkflowSteps.cs
│   │   │   └── Maf/
│   │   │       ├── ConcurrentReviewWorkflow.cs
│   │   │       ├── ReviewStartExecutor.cs
│   │   │       └── ReviewAggregationExecutor.cs
│   │   ├── Interfaces/
│   │   ├── Orchestration/
│   │   │   └── OnboardFlowOrchestrator.cs
│   │   ├── Services/
│   │   │   ├── PiiRedactionService.cs
│   │   │   └── SampleDataService.cs
│   │   └── Utilities/
│   │       └── SnapshotTruncator.cs
│   ├── OnboardFlow.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── ValueObjects/
│   └── OnboardFlow.Infrastructure/
│       ├── Data/OnboardFlowDbContext.cs
│       ├── LlmClients/ChatClientLlmClient.cs
│       └── Repositories/
└── frontend/
    ├── .env.example
    ├── index.html
    ├── package.json
    ├── vite.config.ts
    └── src/
        ├── App.tsx
        ├── main.tsx
        ├── index.css
        ├── theme.ts
        ├── components/
        │   ├── HeaderBar.tsx
        │   ├── IntakeStudio.tsx
        │   ├── RunBoard.tsx
        │   ├── OutputDeck.tsx
        │   ├── RunHistory.tsx
        │   └── BackgroundStage.tsx
        ├── hooks/
        │   └── useRunStream.ts
        └── lib/
            ├── api.ts
            ├── presentation.ts
            ├── signalr.ts
            └── types.ts
```

## Workflow Steps

| # | Step | Description |
|---|------|-------------|
| 01 | **IntakeNormalize** | Normalizes whitespace, cleans up formatting, redacts PII |
| 02 | **ExtractProfile** | Extracts structured applicant profile from the onboarding request |
| 03 | **SecurityReview** | Reviews security risks, integration controls, SSO/SCIM concerns |
| 03 | **ComplianceReview** | Checks data residency, regulatory flags, contract terms |
| 03 | **FinanceReview** | Evaluates billing requirements, credit risks, invoice needs |
| 04 | **AggregateFindings** | Merges all reviewer findings into a single Decision Pack |
| 05 | **CustomerNextSteps** | Generates a concise customer-facing next steps message |
| 06 | **FinalPackage** | Formats the output as structured HTML with full audit trail |

Steps 03 (Security, Compliance, Finance) run **concurrently** via MAF's fan-out/fan-in pattern.

## MAF Concurrent Review

The concurrent stage is defined in `ConcurrentReviewWorkflow.cs`:

```csharp
return new WorkflowBuilder(start)
    .WithName("OnboardFlow-ConcurrentReview")
    .AddFanOutEdge(start, [securityReviewer, complianceReviewer, financeReviewer])
    .AddFanInBarrierEdge([securityReviewer, complianceReviewer, financeReviewer], aggregation)
    .WithOutputFrom(aggregation)
    .Build();
```

Each reviewer is a `ChatClientAgent` with domain-specific instructions that returns a structured JSON with risk level, findings, and recommendation.

## API Surface

```http
GET    /api/runs                    # List all runs
GET    /api/runs/{id}               # Get run detail with steps
POST   /api/runs                    # Start a new onboarding run
POST   /api/runs/{id}/rerun         # Re-run from a specific step
POST   /api/runs/{id}/cancel        # Cancel a running workflow
GET    /api/runs/{id}/lineage       # Get run lineage (original + re-runs)
GET    /api/runs/steps              # Get step definitions
GET    /api/samples                 # List sample applicant data
GET    /api/samples/{index}         # Get a specific sample
```

**SignalR Hub:** `/hubs/runs` — real-time step progress, status changes, and completion events.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- An Azure OpenAI resource with a deployed model (e.g. `gpt-4o-mini`)

## Configuration

Copy and fill in your Azure OpenAI credentials in `appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "ApiKey": "<your-api-key>",
    "ModelDeployment": "<your-deployment-name>"
  }
}
```

## Run Backend

```powershell
cd backend
dotnet restore
dotnet run --project OnboardFlow.Api/OnboardFlow.Api.csproj
```

Backend URL: `http://localhost:5099`

Swagger UI available at `http://localhost:5099/swagger` in development mode.

## Run Frontend

```powershell
cd frontend
copy .env.example .env
npm install
npm run dev
```

Frontend URL: `http://localhost:4174`

## Demo Flow

1. Open the UI at `http://localhost:4174`.
2. Select an example applicant from the **Intake Studio**.
3. Click **Submit for review**.
4. Watch the **Review Progress** board update in real-time as each step executes.
5. Observe the three Expert Reviews (Security, Compliance, Finance) running **concurrently**.
6. Once complete, inspect the **Decision Summary** with the onboarding report, reviewer findings, and detailed review data.
7. Browse **Past onboarding reviews** in the sidebar to compare runs.

## Notes

- The SQLite database (`onboardflow.db`) is created automatically on first run.
- To reset all data, stop the backend and delete `onboardflow.db` from the `OnboardFlow.Api` directory.
- PII redaction runs before any data is sent to the LLM.
- Re-runs are supported — you can re-run from any completed step to iterate on results.
