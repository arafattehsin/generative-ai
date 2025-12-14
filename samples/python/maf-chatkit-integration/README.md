# TravelMate - ChatKit Demo

This demo showcases the integration of **Microsoft Agent Framework** with **OpenAI ChatKit**, featuring:

- 🛫 **Real-time Flight Tracking** using [AviationStack API](https://aviationstack.com/)
- 🅿️ **AI-powered Parking Sign Analysis** using Azure OpenAI GPT-4o vision

![Demo Screenshot](docs/screenshot.png)

## Features

### Flight Tracking

- Search by flight number (e.g., QF1, AA100)
- Search by route (departure → arrival airports)
- Real-time status: scheduled, active, landed, cancelled, diverted
- Gate, terminal, and delay information
- Live tracking data for in-flight aircraft (altitude, speed, coordinates)

### Parking Sign Analysis

- Upload a photo of any parking sign
- GPT-4o vision analyses the sign
- Clear YES/NO verdict with confidence level
- Detailed breakdown of restrictions, time limits, and hours
- Practical advice for drivers

## Prerequisites

- [uv](https://docs.astral.sh/uv/) (recommended) or Python 3.11+
- Node.js 18+
- Azure OpenAI with GPT-4o deployment
- AviationStack API key (free tier available)

## Setup

### 1. Install uv (if not already installed)

```bash
# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh

# Windows PowerShell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```

### 2. Install Python Dependencies

```bash
cd samples/python/maf-chatkit-integration

# uv automatically creates venv and installs dependencies (10-100x faster than pip!)
uv sync
```

### 3. Install Frontend Dependencies

```bash
cd frontend
npm install
```

### 4. Configure Environment Variables

Set the following environment variables:

```bash
# Required for Azure OpenAI
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_CHAT_DEPLOYMENT_NAME="gpt-4o"  # Must be GPT-4o for vision
export AZURE_OPENAI_API_VERSION="2024-12-01-preview"

# Required for Flight Tracking
export AVIATIONSTACK_KEY="your_aviationstack_api_key"
```

**Windows PowerShell:**

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_CHAT_DEPLOYMENT_NAME = "gpt-4o"
$env:AZURE_OPENAI_API_VERSION = "2024-12-01-preview"
$env:AVIATIONSTACK_KEY = "your_aviationstack_api_key"
```

### 5. Azure CLI Authentication

This demo uses Azure CLI credential for authentication:

```bash
az login
```

### 6. Get an AviationStack API Key

1. Go to [aviationstack.com](https://aviationstack.com/)
2. Sign up for a free account
3. Copy your API key from the dashboard

> **Note:** The free tier allows 100 requests/month. For production use, consider a paid plan.

## Running the Demo

### Start the Backend Server

```bash
cd samples/python/maf-chatkit-integration
uv run python app.py
```

The server will start at `http://localhost:8001`.

### Start the Frontend (in a new terminal)

```bash
cd samples/python/maf-chatkit-integration/frontend
npm run dev
```

The frontend will start at `http://localhost:5173`.

### Open the Demo

Navigate to **http://localhost:5173** in your browser.

## Usage Examples

### Flight Tracking

Try these queries:

- "What's the status of flight QF1?"
- "Show me flights from Sydney to Melbourne"
- "Search flights departing from LAX"
- "Check AA100 status"

Or use the interactive widgets:

- Click on airport buttons to search departures
- Click on popular routes for quick searches

### Parking Sign Analysis

1. Click the attachment button (📎) in the composer
2. Upload a photo of a parking sign
3. The AI will analyse and provide:
   - ✅/❌ Clear verdict
   - Time restrictions
   - Day restrictions
   - Detailed explanation
   - Practical advice

## Project Structure

```
maf-chatkit-integration/
├── app.py                 # FastAPI backend server with ChatKitServer
├── flight_widget.py       # Flight status widget components
├── parking_widget.py      # Parking analysis widget components
├── store.py               # SQLite persistence layer (ChatKit Store)
├── attachment_store.py    # File attachment handling (two-phase upload)
├── pyproject.toml         # Python dependencies (uv/pip)
├── frontend/
│   ├── src/
│   │   ├── App.tsx        # ChatKit React component
│   │   └── main.tsx       # Entry point
│   ├── package.json
│   ├── vite.config.ts
│   └── index.html
└── README.md
```

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌──────────────────┐
│                 │     │                 │     │                  │
│  React + Vite   │────▶│   FastAPI       │────▶│  Azure OpenAI    │
│  ChatKit UI     │     │   Backend       │     │  GPT-4o          │
│                 │◀────│                 │◀────│                  │
└─────────────────┘     └────────┬────────┘     └──────────────────┘
                                 │
                                 ▼
                        ┌─────────────────┐
                        │  AviationStack  │
                        │  Flight API     │
                        └─────────────────┘
```

## Key Technologies

- **[Microsoft Agent Framework](https://github.com/microsoft/agent-framework)**: Agent orchestration and tool calling
- **[OpenAI ChatKit](https://github.com/openai/chatkit-python)**: React UI components for chat interfaces
- **[agent-framework-chatkit](https://pypi.org/project/agent-framework-chatkit/)**: Integration bridge between MAF and ChatKit
- **Azure OpenAI GPT-4o**: Vision capabilities for parking sign analysis
- **AviationStack API**: Real-time flight data
- **FastAPI**: Python backend with async support
- **SQLite**: Local persistence for chat history

## How It Works

The integration uses `agent-framework-chatkit` to bridge Microsoft Agent Framework and ChatKit:

1. **ThreadItemConverter**: Converts ChatKit thread items to Agent Framework messages
2. **stream_agent_response()**: Converts Agent Framework streaming output to ChatKit events

```python
from agent_framework_chatkit import ThreadItemConverter, stream_agent_response

# Convert ChatKit messages to Agent Framework format
agent_messages = await converter.to_agent_input(thread_items)

# Run the agent and stream responses back to ChatKit
async for event in stream_agent_response(agent.run_stream(messages), thread_id):
    yield event
```

## Troubleshooting

### "Azure OpenAI is not configured"

- Ensure `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_CHAT_DEPLOYMENT_NAME` are set
- Verify you're logged in with `az login`

### "AVIATIONSTACK_KEY not configured"

- Get a free API key from [aviationstack.com](https://aviationstack.com/)
- Set the `AVIATIONSTACK_KEY` environment variable

### "No flight found"

- Verify the flight number format (e.g., QF1, not QF 1)
- The flight may not exist or may be outside the API's time window
- Free tier only shows current day's flights

### Parking analysis not working

- Ensure the deployment is GPT-4o (vision-capable)
- Check that the image is clear and under 10MB
- Try a different photo with clearer signage

## License

Copyright (c) Microsoft. All rights reserved.
