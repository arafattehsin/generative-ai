# OnboardRoom web UI

React and Vite frontend for the MAF Group Chat Orchestration sample.

## Prerequisites

- Node.js 22.12 or later
- The OnboardRoom API running on `http://localhost:5088`

## Run

```powershell
npm ci
npm run dev
```

Open `http://localhost:5173`.

To point the UI at another API:

```powershell
$env:VITE_API_BASE_URL = "http://localhost:<port>"
npm run dev
```

## Validate

```powershell
npm run lint
npm run build
npm audit --audit-level=high
```
