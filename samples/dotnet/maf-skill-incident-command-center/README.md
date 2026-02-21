# MAF Skill Incident Command Center

A full-stack demo showing how to give agents new capabilities with **Skills** in **Microsoft Agent Framework** using the progressive disclosure pattern.

Scenario: **Supply Chain Disruption Triage** with a phased capability model:

1. `incident-triage` (flagship skill)
2. `incident-communications` (advanced cooperating skill)

## Stack

- Backend: ASP.NET Core (`net10.0`), Microsoft Agent Framework, Azure OpenAI
- Frontend: React + Vite + TypeScript
- Skills: file-based skill packages loaded via `FileAgentSkillsProvider`

## Project Layout

```text
maf-skill-incident-command-center/
├── backend/
│   ├── Program.cs
│   ├── Models/
│   ├── Services/
│   ├── data/incidents/*.json
│   ├── skills/
│   │   ├── incident-triage/
│   │   └── incident-communications/
│   └── tests/IncidentCommandCenter.Api.Tests/
├── frontend/
│   └── src/
└── README.md
```

## Skill Packages

### `incident-triage`

Purpose: classify severity, identify probable causes, and output prioritized actions.

Resources:

- `references/SLA_POLICY.md`
- `references/ESCALATION_MATRIX.md`
- `references/VENDOR_CONSTRAINTS.md`
- `assets/triage-report-template.md`
- `assets/executive-brief-template.md`

### `incident-communications`

Purpose: draft audience-specific updates from triage output.

Resources:

- `references/tone-guidelines.md`
- `assets/customer-update-template.md`
- `assets/supplier-escalation-template.md`

## API Surface

```http
GET  /api/incidents
POST /api/sessions
POST /api/triage
POST /api/communications/draft
GET  /api/skills
GET  /api/runs/{runId}/events
```

## Environment Variables

Required:

- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_DEPLOYMENT_NAME`

Optional:

- `AZURE_OPENAI_API_KEY` (if omitted, `DefaultAzureCredential` is used)

## Run Backend

```powershell
cd backend
$env:AZURE_OPENAI_ENDPOINT="https://<your-endpoint>.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
# Optional:
# $env:AZURE_OPENAI_API_KEY="<your-key>"
dotnet restore
dotnet run
```

Backend default URL: `http://localhost:5000`

## Run Frontend

```powershell
cd frontend
copy .env.example .env
npm install
npm run dev
```

Frontend URL: `http://localhost:5173`

## Demo Flow

1. Open command center UI.
2. Select an incident from the queue.
3. Click **Run Triage**.
4. Inspect skill timeline (`advertised -> loaded -> resource_read -> completed`).
5. Open evidence drawer to see exact resources used.
6. Trigger **Draft Stakeholder Update** to run the second skill.

## Test Cases

### Backend tests

```powershell
cd backend\tests\IncidentCommandCenter.Api.Tests
dotnet test
```

Coverage includes:

- Skill discovery returns both skills
- Incident repository seed/data lookups
- Skill event ordering
- Missing environment variable validation

### Frontend checks

Manual checks:

1. Incident queue loads and can select an incident.
2. Triage call returns structured response card.
3. Timeline shows progressive disclosure events.
4. Evidence drawer lists resources read.
5. Draft communication flow works with audience selector.

## Manual Scenarios

1. Port delay incident with SLA breach risk (`INC-SC-1001`)
2. Supplier shortage incident with substitution pressure (`INC-SC-1002`)
3. Customs hold incident with lower immediate risk (`INC-SC-1003`)

## Notes

- No mock mode is included in this sample by design.
- Local incident JSON keeps onboarding deterministic while model calls remain real.
