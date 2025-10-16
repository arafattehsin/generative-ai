# Spend Control Dashboard (Agent Framework + Azure AI Foundry)

This sample demonstrates a non‑chat UI that drives an Agent Framework (.NET) backend with function tools and human‑in‑the‑loop approvals. The scenario is finance spend control: getting invoice status and releasing payments with an approval threshold.

## Prerequisites

- .NET 9 SDK
- Node.js 18+ and npm
- Azure OpenAI resource and a Chat Completions model deployment (e.g., `gpt-4.1`)

## Backend Configuration

Set these environment variables before running:

- `AOI_ENDPOINT_SWDN`: Azure OpenAI endpoint (e.g., `https://<your-aoai-endpoint>.openai.azure.com/`)
- `AOI_KEY_SWDN`: Azure OpenAI API key
- Optional: `AzureOpenAI__Deployment` to override the default deployment name (`gpt-4.1`).

## Run Backend

```pwsh
$env:AOI_ENDPOINT_SWDN="https://<your-aoai-endpoint>.openai.azure.com/"
$env:AOI_KEY_SWDN="<your-aoai-key>"
dotnet run --project .\learn\spend-dashboard\backend\SpendDashboard.Api.csproj
```

The API listens on `http://localhost:5071` by default and enables CORS for `http://localhost:5173`.

## Run Frontend

```pwsh
cd .\learn\spend-dashboard\frontend
# Optional if backend runs elsewhere
# copy .env.example to .env and set VITE_API_BASE_URL
npm install
npm run dev
```

The app runs at Vite’s dev URL (typically `http://localhost:5173`).

## How It Works

- On load, the frontend calls `POST /api/session` and stores a `sessionId` in `localStorage`.
- `GET /api/invoices` returns seeded invoices.
- Getting status calls `POST /api/actions/get-status` and shows a status drawer.
- Releasing payment calls `POST /api/actions/release-payment`:
  - For amounts ≤ $1,000: payment is auto‑released and the table updates to Paid.
  - For amounts > $1,000: the backend/agent returns a function approval request which the UI presents in a modal.
- Submitting the approval calls `POST /api/approvals`; the agent resumes on the same thread and completes the action.

## API Summary

- `POST /api/session` → `{ sessionId }`
- `GET /api/invoices` → `Invoice[]`
- `POST /api/actions/get-status` → `{ result: InvoiceStatusResult, userInputRequests: [] }`
- `POST /api/actions/release-payment` → `{ result: PaymentResult }` or `{ result: null, userInputRequests: ApprovalRequest[] }`
- `POST /api/approvals` → `{ result: PaymentResult, userInputRequests: [] }`

## Troubleshooting

- Model not found/forbidden: verify `AOI_ENDPOINT_SWDN`, `AOI_KEY_SWDN`, and a Chat model (e.g., `gpt-4.1`) exists.
- CORS: if frontend runs on a different origin, update the backend CORS origin in `Program.cs`.
- API base URL: set `frontend/.env` with `VITE_API_BASE_URL` if backend URL differs from default.
