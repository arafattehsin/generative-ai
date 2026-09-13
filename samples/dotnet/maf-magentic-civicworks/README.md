# CivicWorks: live Magentic council investigation

CivicWorks is a full-stack Microsoft Agent Framework sample for a fictional Australian local-council preliminary works investigation. A live Magentic manager coordinates specialist agents, pauses for an officer to approve the initial and revised plans, reacts to a tool-backed evidence conflict, and produces a cited A/B/C options brief.

The React interface is backed by an ASP.NET Core API and SignalR. Every manager and specialist turn uses the configured Microsoft Foundry model. The case records are fixed, synthetic, read-only tool data; they are not real council records.

## No fallback guarantee

This sample has no mock-agent mode, scripted orchestration, cached completion, or automatic simulation fallback.

- Missing endpoint, model, or Azure CLI authentication causes the run to fail visibly.
- A failed model or workflow call causes the run to fail visibly.
- The final brief must be valid model output with options A, B, and C, a supported recommendation, and evidence-linked claims. Invalid output is rejected rather than replaced with static copy.
- `GET /api/config` always reports `simulationFallbackEnabled: false` and lists any missing live settings.

The synthetic evidence tools are deterministic so that the demo remains safe and repeatable. They supply facts to the live agents; they do not supply agent dialogue, plans, reasoning, or a fallback brief.

## What the demo shows

1. The Magentic manager proposes Plan 1 and waits for an officer decision.
2. Community, access, civil-assets, and place specialists query role-specific read-only tools.
3. `OBS-07` conflicts with `AR-DN-44`, invalidating the direct-works route.
4. The workflow performs one real reset/replan and waits for Plan 2 approval.
5. Revised checks add heritage-register, non-invasive survey, cost/disruption, and independent evidence-verification records.
6. The live manager returns a strict, cited preliminary options brief. This is advice for review, not a works approval.

## Prerequisites

- .NET 10 SDK
- Node.js 20 or later
- Azure CLI
- Access to a Microsoft Foundry project and a deployed chat model

Authenticate with the tenant that can access the Foundry project:

```powershell
az login
az account show
```

The backend deliberately uses `AzureCliCredential` only. It does not fall through to another credential source.

## Configure and run

From this sample directory, start the backend in one PowerShell terminal:

```powershell
$env:MICROSOFT_FOUNDRY_PROJECT_ENDPOINT = '<your-project-endpoint>'
$env:FOUNDRY_MODEL = '<your-model-deployment-name>'

dotnet run --project .\backend\CivicWorks.Api\CivicWorks.Api.csproj
```

The API listens on `http://localhost:5188`. Confirm the live-only configuration before starting a case:

```powershell
Invoke-RestMethod http://localhost:5188/api/config
```

In a second terminal:

```powershell
Set-Location .\frontend
npm install
npm run dev
```

Open `http://localhost:4177`, select **Start investigation**, and approve each plan only after reviewing it.

The investigation workspace shows the current stage, the manager's live plan, its
specialist assignments, and a timestamped activity history. Read the plan, confirm
**I have reviewed this plan and its constraints**, then select **Approve Plan 01**.
Repeat the review when the evidence triggers a revised plan. Previous plan text
and approval times remain available in the plan-version selector.

Use **Evidence register** to search the returned records and inspect each finding.
After completion, **Options brief** shows all three options, the recommendation,
unresolved matters, and clickable evidence references for every structured claim.
The activity panel can be filtered to evidence returns, with expandable assignment
details. These are actual workflow updates; the UI does not generate agent narration.

Refreshing the same browser tab reconnects to its run and restores the server's
activity history. Only the run ID is kept in session storage. A backend restart
clears demo runs. The 10-minute elapsed-time budget includes officer review.

To use a different API origin, set `VITE_API_BASE_URL` before starting Vite.

## Validate locally

```powershell
dotnet build .\backend\CivicWorks.slnx -c Release
dotnet test .\backend\CivicWorks.slnx -c Release --no-build

Set-Location .\frontend
npm run lint
npm run build
npm audit --audit-level=high
```

## Runtime boundaries

- Case: fictional `CW-2047`, Marrin Precinct
- Maximum manager rounds: 12
- Maximum read-only tool calls: 24
- Maximum elapsed time: 10 minutes
- Maximum replans/resets: 1
- Human review gates: initial plan and revised plan
- API: `POST /api/runs`, `GET /api/runs/{id}`, `POST /api/runs/{id}/plan-review`
- Live updates: SignalR at `/hubs/civicworks`

The API stores run state in memory for the demo. Restarting the backend clears prior runs.
