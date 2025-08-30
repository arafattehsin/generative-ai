# A2A Customer Service Configuration

## Environment Variables

Set these environment variables to use your Azure OpenAI service:

```bash
AOI_ENDPOINT_SWDN=https://your-openai-resource.openai.azure.com/
AOI_KEY_SWDN=your-api-key-here
```

## A2A Protocol 0.3.0 Compliance

This application is fully compliant with **A2A Protocol version 0.3.0**. Key compliance features:

### Agent Discovery Endpoints

- `/frontdesk/.well-known/agent-card.json` - Front Desk Agent Card
- `/billing/.well-known/agent-card.json` - Billing Agent Card
- `/technical/.well-known/agent-card.json` - Technical Agent Card
- `/orchestrator/.well-known/agent-card.json` - Orchestrator Agent Card

### Transport Support

- **JSON-RPC 2.0**: Native A2A protocol support
- **HTTP REST**: Standard HTTP API compatibility
- **Dual Transport**: Each agent supports both transport methods

### Protocol Features

- ✅ Protocol version 0.3.0 in Agent Cards
- ✅ Well-known endpoint discovery with `agent-card.json` (0.3.0 breaking change)
- ✅ Dual transport announcement (`preferredTransport`)
- ✅ Comprehensive skill definitions with input/output schemas
- ✅ Provider information and documentation URLs
- ✅ Agent capabilities and interface declarations

## Runtime Toggle

Switch between mock and real implementations:

- **Frontend**: Use the toggle switch in the "Implementation Mode" card
- **API**: `POST /api/customerservice/toggle-implementation`

## Configuration Priority

1. Runtime toggle (highest)
2. Environment variables
3. appsettings.json (fallback)

Perfect for .NET! 🚀
