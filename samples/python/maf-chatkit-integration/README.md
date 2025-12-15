# SwiftRover - ChatKit Demo

A sample demonstrating **Microsoft Agent Framework** integrated with **OpenAI ChatKit**, featuring real-time flight tracking, AI-powered parking sign analysis, and expense analysis with o3 reasoning.

📖 **Read the full blog post**: [Building modern chat experiences with Microsoft Agent Framework and OpenAI ChatKit](https://arafattehsin.com/blog/agent-framework-chatkit-integration/)

![Demo Screenshot](docs/screenshot.png)

## Features

- 🛫 **Flight Tracking** - Real-time status via AviationStack API with interactive widgets
- 🅿️ **Parking Sign Analysis** - GPT-5.1 vision analyses parking signs and provides verdicts
- 💰 **Expense Analysis** - o3 reasoning model with streaming "Thought for X seconds" workflow

## Prerequisites

- [uv](https://docs.astral.sh/uv/) (recommended) or Python 3.11+
- Node.js 18+
- Azure OpenAI with GPT-5.1 and o3 deployments
- [AviationStack API key](https://aviationstack.com/) (free tier available)

## Quick Start

### 1. Install Dependencies

```bash
cd samples/python/maf-chatkit-integration

# Backend (Python)
uv sync

# Frontend (Node.js)
cd frontend && npm install && cd ..
```

### 2. Set Environment Variables

```powershell
# Windows PowerShell
$env:AOI_ENDPOINT_SWDN = "https://your-resource.openai.azure.com/"
$env:AOI_KEY_SWDN = "your-api-key"  # Or use Azure CLI auth
$env:AVIATIONSTACK_KEY = "your_aviationstack_api_key"
```

```bash
# macOS/Linux
export AOI_ENDPOINT_SWDN="https://your-resource.openai.azure.com/"
export AOI_KEY_SWDN="your-api-key"
export AVIATIONSTACK_KEY="your_aviationstack_api_key"
```

### 3. Run the Demo

```bash
# Terminal 1 - Backend
uv run python app.py
# Starts at http://localhost:8001

# Terminal 2 - Frontend
cd frontend && npm run dev
# Starts at http://localhost:5173
```

### 4. Open in Browser

Navigate to **http://localhost:5173**

## Usage

| Feature          | How to Use                                                             |
| ---------------- | ---------------------------------------------------------------------- |
| Flight Tracking  | Ask "What's the status of flight QF1?" or use airport selector widgets |
| Parking Analysis | Upload a parking sign image via the 📎 button                          |
| Expense Analysis | Ask "Analyse Q4 expenses" to see o3 reasoning in action                |

## Project Structure

```
maf-chatkit-integration/
├── app.py                 # FastAPI backend with ChatKitServer
├── flight_widget.py       # Flight status widgets
├── parking_widget.py      # Parking analysis widgets
├── store.py               # SQLite persistence
├── attachment_store.py    # File upload handling
├── pyproject.toml         # Python dependencies
└── frontend/              # React + Vite + ChatKit UI
```

## Learn More

For architecture details, integration patterns, and implementation walkthrough, read the blog post:

👉 [Building modern chat experiences with Microsoft Agent Framework and OpenAI ChatKit](https://arafattehsin.com/blog/agent-framework-chatkit-integration/)

## License

Copyright (c) Microsoft. All rights reserved.
