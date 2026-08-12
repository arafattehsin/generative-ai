# MAF Handoff Orchestration: Live Recovery

A full-stack sample for **Agent Orchestration Patterns - Handoff**. It follows one disrupted traveller case through AI-to-AI ownership changes and a real human decision boundary.

This is not a generic operations dashboard. It has two deliberate experiences:

- **Maya Chen**, the traveller, uses a quiet recovery chat at `http://localhost:5173/`.
- **Alex Morgan**, a recovery employee, claims and resolves human decisions at `http://localhost:5173/operations`.

Both views operate on the same case. Maya sees continuity and the current owner. Alex sees only the context required to make an authorised decision.

## What The Sample Demonstrates

```text
Maya
  |
  v
Journey Triage --> Flight Recovery
                          |
                    RequestPort pause
                          |
                          v
                     Alex Morgan
                          |
                          v
                     Travel Cover
```

The AI-to-AI changes use Microsoft Agent Framework Handoff orchestration. When Flight Recovery reaches work that requires a person, the application creates a typed `RequestPort<HumanSupportRequest, HumanSupportResolution>` request. The workflow remains suspended until Alex claims the case and responds. Alex's message then appears in Maya's transcript before ownership returns to the selected AI specialist.

The sample always calls Microsoft Foundry. There is no preview or scripted-response mode.

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- A Microsoft account with access to a Microsoft Foundry project

## Configuration

Set the Foundry project endpoint before starting the API:

| Setting | Purpose |
| --- | --- |
| `MICROSOFT_FOUNDRY_PROJECT_ENDPOINT` | Microsoft Foundry project endpoint |
| `AZURE_TENANT_ID` | Optional tenant override for local browser sign-in |
| `FOUNDRY_AUTH_MODE` | Optional: `interactive` for browser sign-in or `default` for managed identity/environment credentials |

The model/deployment name is hardcoded as `gpt-5.4`.

```powershell
$env:MICROSOFT_FOUNDRY_PROJECT_ENDPOINT = "https://<your-project>.services.ai.azure.com/api/projects/<project-name>"
```

Local development uses a persistent interactive browser credential by default. The first live chat may open Microsoft sign-in. Azure Identity keeps the encrypted token cache for the Windows user, and the sample stores only the selected account record at `%LOCALAPPDATA%\TravelConcierge\foundry-auth-record.json`. Later API restarts should authenticate silently.

Hosted environments should use `DefaultAzureCredential`. Set `FOUNDRY_AUTH_MODE=default` for managed identity, environment credentials or CI.

## Run Locally

From this sample folder, start the API:

```powershell
dotnet restore .\backend\TravelConcierge.slnx
dotnet run --project .\backend\TravelConcierge.Api\TravelConcierge.Api.csproj
```

In a second terminal, start the React application:

```powershell
cd .\frontend
npm install
npm run dev
```

Open the two experiences in separate browser tabs:

```text
Traveller: http://localhost:5173/
Operations: http://localhost:5173/operations
API:        http://localhost:5166
```

If `5173` is busy, Vite prints the port it selected. The API allows the common fallback ports `5174` and `5175`.

## Complete Demo Walkthrough

### 1. Start Maya's case

Open the traveller experience. Leave the default **Cancelled Last Flight Home** scenario selected and click **Start chat**.

Observe that Journey Triage receives Maya's request and hands ownership to Flight Recovery. The active owner changes in the route summary, conversation header and ownership timeline.

### 2. Answer Flight Recovery

When Flight Recovery asks whether Maya can take another airline or a connection, send:

```text
Yes. I can take another airline or a connecting flight, but I need a confirmed arrival before 2 PM tomorrow.
```

Flight Recovery should remain the owner and ask whether Qantas has offered a confirmed alternative.

### 3. Request a person

Send:

```text
No. Qantas has not offered a confirmed alternative. Please connect me with a person who can authorise the fastest option before my medical appointment.
```

Maya's reply box becomes read-only and the UI says that Recovery Operations has been requested. This is the live `RequestPort` pause, not a simulated chat response.

### 4. Let Alex claim the decision

Switch to the operations tab. Maya's critical case should appear in the priority queue with:

- the requesting AI specialist;
- the reason a person is needed;
- the exact decision Alex must make;
- recent conversation context;
- a concise AI recommendation.

Click **Take ownership**. Maya's view now identifies Alex Morgan as the recovery specialist reviewing her case.

### 5. Return ownership to Travel Cover

Write Alex's decision in the empty response field, leave **Travel Cover** selected and click **Send decision to Maya**.

The operations queue clears. In Maya's chat, Alex's response appears under his name and initials. The ownership timeline shows the human step, then Travel Cover becomes the current owner.

### 6. Continue with the next specialist

From Maya's chat, send:

```text
Qantas will not provide a hotel voucher. What exactly should I keep for my insurance claim?
```

Travel Cover should reply with a short **Save for your claim** card containing three evidence categories. It should explicitly say that Maya does not upload anything in this demo.

## What To Observe

The important result is not the travel advice. It is the ownership behaviour:

1. Triage routes instead of answering every topic.
2. Flight Recovery keeps ownership while it asks Maya for missing information.
3. Work leaves the AI mesh when authority is required.
4. Alex is a named person with a separate, role-appropriate interface.
5. Alex's decision becomes part of the same customer conversation.
6. Ownership resumes with Travel Cover without Maya repeating the case.

## Scope

The Foundry calls, Agent Framework handoffs and RequestPort pause are real. Airline inventory, bookings, hotel approval, payments, document upload and insurance claim submission are intentionally simulated.

The human request uses `CheckpointManager.Default` and in-process state so the orchestration remains easy to inspect. A production implementation would use durable workflow checkpoints, an authenticated operator queue, role-based access and restart recovery.

## Verification

```powershell
dotnet test .\backend\TravelConcierge.slnx
npm run build --prefix .\frontend
```
