# MAF Group Chat Orchestration (OnboardRoom)

A console-first sample for **Agent Orchestration Patterns - Part 4**. It demonstrates a chair-led group chat using Microsoft Agent Framework workflows, Microsoft Foundry agents through `AIProjectClient`, and Foundry Toolbox over MCP.

## What It Builds

```text
User onboarding request
        |
        v
ChairLedGroupChatManager
        |
        +--> OnboardRoom_Intake
        +--> OnboardRoom_Benefits   -- Foundry Toolbox MCP tools
        +--> OnboardRoom_Access     -- Foundry Toolbox MCP tools
        +--> OnboardRoom_Policy     -- Foundry Toolbox MCP tools
        +--> OnboardRoom_Chair
```

The console app creates short-lived hosted Foundry agent versions for the run, wraps them as `FoundryAgent` instances, connects to a Foundry Toolbox MCP endpoint, and streams the group chat output in the terminal.

## Prerequisites

- .NET 10 SDK
- Azure login with access to the target Foundry project
- The adjacent `agent-framework` repo checked out at `C:\Users\AT\source\repos\agent-framework`

## Configuration

Configure your own Microsoft Foundry project before running the console or API:

| Setting | Purpose |
| --- | --- |
| `FOUNDRY_PROJECT_ENDPOINT` or `AZURE_AI_PROJECT_ENDPOINT` | Microsoft Foundry project endpoint |
| `FOUNDRY_MODEL` or `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Model deployment name |
| `FOUNDRY_TOOLBOX_NAME` | Existing toolbox name; defaults to `onboardroom-toolbox` |
| `FOUNDRY_TOOLBOX_API_VERSION` | Toolbox API version; defaults to `v1` |

For the API project, you can also use hierarchical .NET configuration keys such as `Foundry:ProjectEndpoint` and `Foundry:DeploymentName`.

## Run

Build first:

```powershell
dotnet build .\backend\OnboardRoom.slnx
```

Run against an existing toolbox:

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
- The sample uses local Agent Framework project references so it stays aligned with the adjacent latest source checkout.
