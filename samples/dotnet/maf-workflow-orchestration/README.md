# PolicyPack Builder

A production-quality demo application showcasing **Microsoft Agent Framework Sequential Workflows** with **Microsoft Foundry** backend. Transform draft policies into polished, compliant communications through a 6-step AI-powered workflow.

![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![React](https://img.shields.io/badge/React-18-blue)
![TypeScript](https://img.shields.io/badge/TypeScript-5-blue)

## 🎯 Overview

PolicyPack Builder demonstrates enterprise-grade AI workflow orchestration using:

- **Microsoft Agent Framework** - Sequential workflow pattern with 6 orchestrated steps
- **Microsoft Foundry** - Azure OpenAI backend with API key authentication
- **Clean Architecture** - Domain → Application → Infrastructure → API layers
- **Real-time Updates** - SignalR for live step progress tracking
- **SQLite Persistence** - Workflow runs with snapshot storage and re-run lineage
- **Modern UI** - React + TypeScript + Mantine components

### Workflow Steps

1. **Intake + Normalize** - Text cleanup and PII redaction
2. **Extract Facts** - LLM-powered structured fact extraction
3. **Draft Summary** - Audience-appropriate summary generation
4. **Compliance Check** - Hybrid LLM + rules-based validation
5. **Brand Tone Rewrite** - Tone adjustment while preserving compliance
6. **Final Package** - Styled HTML output generation

## 📋 Prerequisites

- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **Microsoft Foundry Project** - With Azure OpenAI deployment (gpt-4o recommended)

## 🚀 Quick Start

### 1. Clone Repository

```bash
cd samples/dotnet/maf-workflow-orchestration
```

### 2. Configure Backend

Create environment variables (or add to `appsettings.json`):

**Windows (PowerShell):**

```powershell
$env:MICROSOFT_FOUNDRY_PROJECT_ENDPOINT = "https://your-foundry-endpoint.openai.azure.com/"
$env:MICROSOFT_FOUNDRY_PROJECT_KEY = "your-api-key-here"
$env:AZURE_OPENAI_DEPLOYMENT = "gpt-4o"  # Optional, defaults to gpt-4o
```

**Linux/Mac:**

```bash
export MICROSOFT_FOUNDRY_PROJECT_ENDPOINT="https://your-foundry-endpoint.openai.azure.com/"
export MICROSOFT_FOUNDRY_PROJECT_KEY="your-api-key-here"
export AZURE_OPENAI_DEPLOYMENT="gpt-4o"
```

Alternatively, update `backend/PolicyPackBuilder.Api/appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-foundry-endpoint.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4o"
  }
}
```

### 3. Run Backend

```bash
cd backend/PolicyPackBuilder.Api
dotnet restore
dotnet run
```

Backend will start on **http://localhost:5000**

- API: `http://localhost:5000/api`
- Swagger: `http://localhost:5000/swagger`
- SignalR Hub: `http://localhost:5000/hubs/runs`

### 4. Run Frontend

Open a new terminal:

```bash
cd frontend
npm install
npm run dev
```

Frontend will start on **http://localhost:5173**

### 5. Try It Out!

1. Navigate to http://localhost:5173
2. Click a sample ("HR Policy" or "Product Warranty")
3. Choose audience, tone, and compliance settings
4. Click **Generate PolicyPack**
5. Watch real-time step execution
6. View the final HTML output
7. Try **Re-run from here** on any step to see lineage tracking

## 🏗️ Architecture

### Backend Structure

```
backend/
├── PolicyPackBuilder.Domain/         # Entities, Enums, Value Objects
│   ├── Entities/
│   │   ├── WorkflowRun.cs            # Main run entity with ParentRunId/RootRunId
│   │   └── WorkflowStepRun.cs        # Step execution with snapshots
│   ├── Enums/                        # WorkflowStatus, StepStatus, etc.
│   └── ValueObjects/                 # WorkflowOptions, WorkflowContext
│
├── PolicyPackBuilder.Application/    # Business Logic
│   ├── Executors/                    # 6 Step Executors
│   │   ├── IntakeNormalizeExecutor.cs
│   │   ├── ExtractFactsExecutor.cs
│   │   ├── DraftSummaryExecutor.cs
│   │   ├── ComplianceCheckExecutor.cs
│   │   ├── BrandToneRewriteExecutor.cs
│   │   └── FinalPackageExecutor.cs
│   ├── Orchestration/
│   │   └── WorkflowOrchestrator.cs   # Sequential execution engine
│   ├── Services/                     # PII, Compliance, Samples
│   ├── Interfaces/                   # Repository and service contracts
│   └── DTOs/                         # API request/response models
│
├── PolicyPackBuilder.Infrastructure/ # Data Access
│   ├── Data/
│   │   └── PolicyPackDbContext.cs    # EF Core SQLite context
│   ├── Repositories/                 # Run and Step repositories
│   └── LlmClients/
│       └── ChatClientLlmClient.cs    # Azure OpenAI wrapper
│
└── PolicyPackBuilder.Api/            # REST API + SignalR
    ├── Controllers/
    │   ├── RunsController.cs         # Workflow CRUD + lineage
    │   └── SamplesController.cs      # Static sample data
    ├── Hubs/
    │   └── RunsHub.cs                # Real-time progress updates
    └── Program.cs                    # DI configuration
```

### Frontend Structure

```
frontend/
├── src/
│   ├── api/
│   │   ├── client.ts                 # REST API client
│   │   └── signalr.ts                # SignalR hub connection
│   ├── types/
│   │   └── index.ts                  # TypeScript types
│   ├── pages/
│   │   ├── HomePage.tsx              # Create run + samples
│   │   └── RunDetailsPage.tsx        # Step progress + output
│   └── App.tsx                       # Router + providers
```

## 🔧 Configuration

### Environment Variables

| Variable                             | Required | Default  | Description                |
| ------------------------------------ | -------- | -------- | -------------------------- |
| `MICROSOFT_FOUNDRY_PROJECT_ENDPOINT` | Yes      | -        | Azure OpenAI endpoint URL  |
| `MICROSOFT_FOUNDRY_PROJECT_KEY`      | Yes      | -        | API key for authentication |
| `AZURE_OPENAI_DEPLOYMENT`            | No       | `gpt-4o` | Deployment/model name      |

### Frontend Environment Variables

Create `frontend/.env`:

```env
VITE_API_URL=http://localhost:5000/api
VITE_HUB_URL=http://localhost:5000/hubs/runs
```

## 📊 Features

### ✅ Core Features

- **Sequential Workflow Orchestration** - 6-step pipeline with dependency management
- **Re-run from Any Step** - Creates new WorkflowRun with lineage tracking (ParentRunId/RootRunId)
- **Real-time Progress** - SignalR events for step started/completed/failed
- **Snapshot Persistence** - SQLite storage with 50KB truncation for large outputs
- **Compliance Rules Engine** - Restricted phrases, required disclaimers by audience
- **PII Redaction** - Email and phone number detection/masking
- **Sample Data** - HR Policy and Product Warranty with intentional compliance issues

### 🎨 UI Features

- **Mantine Components** - Modern, accessible design system
- **Vertical Stepper** - Visual progress with status indicators
- **Live Updates** - Automatic refresh via SignalR
- **Output Tabs** - Input/output snapshots with truncation indicators
- **Re-run Menu** - Context menu on each step
- **Lineage Tracking** - View parent/child run relationships

## 🧪 Testing the Demo

### Test Scenario 1: HR Policy with Compliance Issues

1. Load "HR Remote Work Policy" sample
2. Set Audience: **Internal**, Tone: **Friendly**, Strict: **Off**
3. Watch as workflow detects informal language and missing disclaimers
4. Observe compliance check step fixing issues
5. See tone rewrite maintaining compliance fixes

### Test Scenario 2: Product Warranty Re-run

1. Load "SmartWidget Pro 3000 Warranty" sample
2. Set Audience: **Customer**, Tone: **Professional**, Strict: **On**
3. Let workflow complete
4. Click "Re-run from here" on **Compliance Check** step
5. Notice new run created with ParentRunId
6. Toggle Strict Compliance and see different disclaimers

### Test Scenario 3: Real-time Monitoring

1. Create a run
2. Open browser DevTools → Network → WS tab
3. Observe SignalR WebSocket messages (StepStarted, StepCompleted events)
4. Open a second browser tab to same run
5. Both tabs update simultaneously via SignalR groups

## 📖 API Documentation

### REST Endpoints

| Method | Endpoint                 | Description                |
| ------ | ------------------------ | -------------------------- |
| `GET`  | `/api/runs`              | List runs (paginated)      |
| `GET`  | `/api/runs/{id}`         | Get run details with steps |
| `POST` | `/api/runs`              | Create new run             |
| `POST` | `/api/runs/{id}/rerun`   | Re-run from specific step  |
| `POST` | `/api/runs/{id}/cancel`  | Cancel running workflow    |
| `GET`  | `/api/runs/{id}/lineage` | Get parent/child runs      |
| `GET`  | `/api/runs/steps`        | Get step definitions       |
| `GET`  | `/api/samples`           | List sample inputs         |

### SignalR Events

| Event           | Payload                                        | Description                |
| --------------- | ---------------------------------------------- | -------------------------- |
| `StepStarted`   | `{ runId, stepName, startedAt }`               | Step began execution       |
| `StepCompleted` | `{ runId, stepName, durationMs, completedAt }` | Step finished successfully |
| `StepFailed`    | `{ runId, stepName, error, failedAt }`         | Step encountered error     |
| `RunCompleted`  | `{ runId, success, error?, completedAt }`      | Workflow finished          |

Visit **http://localhost:5000/swagger** for interactive API documentation.

## 🔍 Key Implementation Details

### Re-run Mechanism

**NOT using MAF CheckpointManager** - This demo intentionally uses SQLite persistence to demonstrate custom state management:

1. User clicks "Re-run from here" on a step
2. System creates **NEW WorkflowRun** with `ParentRunId` pointing to original run
3. Orchestrator reconstructs context by loading `OutputSnapshot` from parent's last completed step
4. Workflow executes from selected step forward
5. Lineage API shows full run chain (original → rerun → rerun...)

### Snapshot Truncation

All step input/output snapshots are limited to **50,000 characters**:

```csharp
public static TruncationResult Truncate(string text, int maxLength = DefaultMaxLength)
{
    if (text.Length <= maxLength)
        return new TruncationResult { Text = text, IsTruncated = false, FullLength = text.Length };

    return new TruncationResult
    {
        Text = text.Substring(0, maxLength) + TruncationMarker,
        IsTruncated = true,
        FullLength = text.Length
    };
}
```

Frontend displays truncation warnings with full length indicator.

### Clean Architecture Benefits

- **Domain Layer** - Pure business entities, no dependencies
- **Application Layer** - Business logic, interfaces, executors
- **Infrastructure Layer** - External concerns (DB, Azure OpenAI)
- **API Layer** - HTTP/SignalR endpoints

This separation enables:

- Easy unit testing (mock interfaces)
- Swappable implementations (SQLite → Cosmos DB)
- Clear dependency flow (outer layers depend on inner)

## 🐛 Troubleshooting

### Backend won't start

**Error:** `InvalidOperationException: MICROSOFT_FOUNDRY_PROJECT_ENDPOINT is not set`

**Solution:** Set environment variables or update `appsettings.json`

### Frontend API calls fail

**Error:** `CORS policy: No 'Access-Control-Allow-Origin' header`

**Solution:** Verify backend is running on port 5000. CORS is configured for `localhost:5173` and `localhost:3000`.

### SignalR disconnects

**Error:** `Error: Connection disconnected with error 'Error: WebSocket closed with status code: 1006'.`

**Solution:** Check backend SignalR hub is mapped in `Program.cs`:

```csharp
app.MapHub<RunsHub>("/hubs/runs");
```

### Database errors

**Error:** `Microsoft.Data.Sqlite.SqliteException: SQLite Error 1: 'no such table: WorkflowRuns'`

**Solution:** EF Core auto-creates schema on first run. Delete `policypack.db` and restart backend.

## 🎓 Learning Resources

- [Microsoft Agent Framework Docs](https://github.com/microsoft/agent-framework)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [SignalR with ASP.NET Core](https://learn.microsoft.com/aspnet/core/signalr/introduction)
- [Clean Architecture Pattern](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Mantine UI Components](https://mantine.dev/)

## 📝 License

This sample is part of the Microsoft Agent Framework project and follows the same license.

## 🤝 Contributing

This is a demo application for blog post purposes. For production use cases, consider:

- Adding authentication/authorization (Azure AD, API keys per user)
- Migrating from SQLite to Azure Cosmos DB or SQL Server
- Implementing proper logging and monitoring (Application Insights)
- Adding retry policies and circuit breakers
- Using MAF CheckpointManager for production workflow resumption
- Adding comprehensive unit and integration tests

---

**Built with ❤️ using Microsoft Agent Framework**
