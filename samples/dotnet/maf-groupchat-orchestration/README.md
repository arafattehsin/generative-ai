# MAF Group Chat Orchestration (OnboardRoom)

A full-stack reference sample for **Agent Orchestration Patterns - Part 4**. OnboardRoom combines a React operations UI, an ASP.NET Core API, SignalR streaming, SQLite persistence, and a chair-led Microsoft Agent Framework Group Chat backed by Microsoft Foundry agents and Foundry Toolbox over MCP.

> **Blog post:** [Group Chat Orchestration: Building a Moderated Boardroom with Microsoft Agent Framework](https://arafattehsin.com/blog/agent-orchestration-patterns-part-4/)

## What It Builds

```text
React operations UI
        |
        | HTTP + SignalR
        v
ASP.NET Core API
        |
        +--> SQLite run history, step snapshots, reruns, and lineage
        |
        v
RunOrchestrator
        |
        +--> 1. Intake normalization and PII redaction
        +--> 2. Applicant profile extraction
        +--> 3. Boardroom review
        |        |
        |        v
        |    ChairLedGroupChatManager
        |        |
        |        +--> OnboardRoom_Intake
        |        +--> OnboardRoom_Benefits   -- Foundry Toolbox MCP tools
        |        +--> OnboardRoom_Access     -- Foundry Toolbox MCP tools
        |        +--> OnboardRoom_Policy     -- Foundry Toolbox MCP tools
        |        +--> OnboardRoom_Chair
        +--> 4. Chair recommendation
        +--> 5. Customer next steps
        +--> 6. HTML export package
```

The primary experience is the API and web UI: submit an onboarding request, watch workflow and speaker events stream live, inspect every step, review the Chair's recommendation, rerun from a selected step, and export the final package.

The repository also includes a console runner for exercising the Group Chat directly. It connects to the same Foundry Toolbox MCP boundary, creates short-lived Foundry agent versions, streams the discussion in the terminal, and can optionally create a sample toolbox version.

## Prerequisites

- .NET 10 SDK
- Node.js 22.12 or later
- Azure login with access to the target Foundry project
- Microsoft Agent Framework packages restored from NuGet

Authenticate locally with `az login`. The console and API use `AzureCliCredential` for local development; production hosts should use a specific credential such as managed identity.

## Configuration

Configure your own Microsoft Foundry project before running the console or API:

| Setting | Purpose |
| --- | --- |
| `MICROSOFT_FOUNDRY_PROJECT_ENDPOINT` | Microsoft Foundry project endpoint |
| `FOUNDRY_TOOLBOX_NAME` | Existing toolbox name; defaults to `onboardroom-toolbox` |
| `FOUNDRY_TOOLBOX_API_VERSION` | Toolbox API version; defaults to `v1` |

The sample always uses the `gpt-5.4` model deployment. `FOUNDRY_PROJECT_ENDPOINT`, `AZURE_AI_PROJECT_ENDPOINT`, and the hierarchical .NET key `Foundry:ProjectEndpoint` remain supported as endpoint aliases.

## Validate Without Calling a Model

From the sample directory:

```powershell
dotnet restore .\backend\OnboardRoom.slnx
dotnet build .\backend\OnboardRoom.slnx --configuration Release --no-restore
dotnet test .\backend\OnboardRoom.slnx --configuration Release --no-build
dotnet list .\backend\OnboardRoom.slnx package --vulnerable --include-transitive

cd .\frontend
npm ci
npm run lint
npm run build
npm audit --audit-level=high
```

## Run

### API and web UI

Start the API from the sample directory:

```powershell
dotnet run --project .\backend\OnboardRoom.Api\OnboardRoom.Api.csproj
```

The development API listens on `http://localhost:5088`. In a second terminal:

```powershell
cd .\frontend
npm ci
npm run dev
```

Open `http://localhost:5173`. To use a different API URL, set `VITE_API_BASE_URL` before starting Vite.

### Console runner

Run the focused Group Chat against an existing toolbox:

```powershell
dotnet run --project .\backend\OnboardRoom.Console\OnboardRoom.Console.csproj
```

Create a sample toolbox version first, then run:

```powershell
dotnet run --project .\backend\OnboardRoom.Console\OnboardRoom.Console.csproj -- --create-toolbox
```

Override the request or manager:

```powershell
dotnet run --project .\backend\OnboardRoom.Console\OnboardRoom.Console.csproj -- --manager roundrobin --max-rounds 6 --request "Onboard a new Sydney-based finance analyst with SAP, Teams, and VPN access."
```

## Notes

- `--create-toolbox` creates a new toolbox version with web search, Microsoft Learn MCP, and code interpreter tools. It does not delete existing toolbox versions.
- The console app creates hosted agent versions for each run and deletes them in `finally`.
- The sample uses Microsoft Agent Framework packages restored from NuGet.
- `Microsoft.Agents.AI.Foundry` is currently a prerelease package. The pinned project references restore it automatically; use `--prerelease` when adding it to another project.
- The API persists local run data in `backend/OnboardRoom.Api/onboardroom.db`.

## References

- [Microsoft Agent Framework overview](https://learn.microsoft.com/agent-framework/overview/)
- [Group chat orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/group-chat)
- [Microsoft Foundry provider for Agent Framework](https://learn.microsoft.com/agent-framework/agents/providers/microsoft-foundry)
- [Create and use a Foundry toolbox](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/toolbox)
