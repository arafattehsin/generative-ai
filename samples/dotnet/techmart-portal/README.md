# TechMart Enterprise Operations Portal

A comprehensive enterprise-level demonstration showcasing the Microsoft Agent Framework's distributed agent architecture using .NET Aspire orchestration.

## Overview

TechMart Enterprise Operations Portal demonstrates how to build a scalable, enterprise-grade multi-agent system with:

- **Distributed A2A Agents** - Specialized agents running as separate services communicating via Agent-to-Agent (A2A) protocol
- **.NET Aspire Orchestration** - Service discovery, health monitoring, and telemetry out of the box
- **DevUI Integration** - Interactive testing interface for agent development
- **Human-in-the-Loop Workflows** - Approval gates for critical business operations

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        .NET Aspire AppHost                       │
│                    (Service Orchestration)                       │
└─────────────────────────────────────────────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        │                        │                        │
        ▼                        ▼                        ▼
┌───────────────┐       ┌───────────────┐       ┌───────────────┐
│ Inventory     │       │   Finance     │       │   Support     │
│    Agent      │       │    Agent      │       │    Agent      │
│  (Port 5101)  │       │  (Port 5102)  │       │  (Port 5103)  │
│               │       │               │       │               │
│ • Inventory   │       │ • Pricing     │       │ • Tickets     │
│ • Warehouses  │       │ • Quotes      │       │ • Knowledge   │
│ • Reservations│       │ • Payments    │       │ • Escalation  │
└───────────────┘       └───────────────┘       └───────────────┘
        ▲                        ▲                        ▲
        │          A2A Protocol  │                        │
        └────────────────────────┼────────────────────────┘
                                 │
                                 ▼
                    ┌─────────────────────────┐
                    │      Agent Host         │
                    │      (Port 5100)        │
                    │                         │
                    │ Coordinator Agents:     │
                    │ • operations-coordinator│
                    │ • order-specialist      │
                    │ • account-manager       │
                    │                         │
                    │ Workflows:              │
                    │ • order-fulfillment     │
                    │ • parallel-review       │
                    │                         │
                    │ Endpoints:              │
                    │ • /devui (DevUI)        │
                    │ • /a2a/coordinator      │
                    └─────────────────────────┘
```

## Services

### TechMart.AgentHost (Main Coordinator)

The central orchestration service hosting:

**Agents:**

- **operations-coordinator** - Master coordinator for complex multi-step operations
- **order-specialist** - Specializes in order processing and fulfillment
- **account-manager** - Handles customer account and relationship management

**Workflows:**

- **order-fulfillment** - Sequential workflow: validation → inventory check → payment → shipping
- **parallel-review** - Concurrent review workflow for multi-department approvals

### TechMart.InventoryAgent (A2A Remote)

Specialized inventory management agent with tools:

| Tool                   | Description                          |
| ---------------------- | ------------------------------------ |
| `GetInventoryDetails`  | Get stock levels and product details |
| `GetReorderList`       | Products below reorder threshold     |
| `ReserveInventory`     | Reserve inventory for orders         |
| `CheckWarehouseStatus` | Real-time warehouse capacity         |

**Simulated Warehouses:** Seattle-WH1, Portland-WH2, Denver-WH3

### TechMart.FinanceAgent (A2A Remote)

Financial operations agent with tools:

| Tool                | Description                          |
| ------------------- | ------------------------------------ |
| `GetPricingDetails` | Product pricing and volume discounts |
| `GenerateQuote`     | Create formal quotes for customers   |
| `CheckCreditStatus` | Customer credit limit verification   |
| `ProcessPayment`    | Payment processing and authorization |
| `GenerateInvoice`   | Create invoices for completed orders |

**Customer Tiers:** Enterprise ($100K), Premium ($50K), Standard ($25K)

### TechMart.SupportAgent (A2A Remote)

Customer support agent with tools:

| Tool                  | Description                     |
| --------------------- | ------------------------------- |
| `CreateSupportTicket` | Open new support tickets        |
| `GetTicketStatus`     | Check ticket progress           |
| `GetKnowledgeArticle` | Search knowledge base           |
| `EscalateTicket`      | Escalate to higher support tier |

**SLA Tiers:** Standard (24hr), Premium (4hr), Enterprise (1hr)

## Prerequisites

- .NET 10 SDK
- Azure OpenAI deployment (gpt-4o or equivalent)
- .NET Aspire workload

```powershell
# Install .NET Aspire workload
dotnet workload install aspire
```

## Configuration

Set the following environment variables:

```powershell
$env:AOI_ENDPOINT_SWDN = "https://your-openai.openai.azure.com/"
$env:AOI_KEY_SWDN = "your-api-key"  # Optional if using DefaultAzureCredential
```

## Running the Solution

### Using Aspire (Recommended)

```powershell
cd learn\techmart-enterprise-portal\backend\TechMart.AppHost
dotnet run
```

This starts all services with:

- Aspire Dashboard for monitoring
- Service discovery between agents
- Distributed tracing and logging

### Running Individual Services

```powershell
# Terminal 1: Inventory Agent
cd TechMart.InventoryAgent
dotnet run

# Terminal 2: Finance Agent
cd TechMart.FinanceAgent
dotnet run

# Terminal 3: Support Agent
cd TechMart.SupportAgent
dotnet run

# Terminal 4: Agent Host
cd TechMart.AgentHost
dotnet run
```

## Testing with DevUI

Once running, access DevUI at: `http://localhost:5100/devui`

**Example prompts:**

1. **Inventory Check:**

   > "What's the current inventory status for product SKU-12345?"

2. **Order Processing:**

   > "Process an order for 100 units of SKU-12345 for customer CUST-001"

3. **Complex Workflow:**

   > "I need to fulfill a large order: check inventory across all warehouses, verify customer credit, and create a support ticket for delivery scheduling"

4. **Multi-Agent Coordination:**
   > "Generate a comprehensive quote for enterprise customer ACME Corp including inventory availability, pricing with volume discount, and payment terms"

## Project Structure

```
techmart-enterprise-portal/
├── backend/
│   ├── TechMart.sln
│   ├── Directory.Packages.props
│   ├── TechMart.AppHost/           # Aspire orchestrator
│   ├── TechMart.ServiceDefaults/   # Shared Aspire config
│   ├── TechMart.AgentHost/         # Main coordinator service
│   ├── TechMart.InventoryAgent/    # Remote A2A agent
│   ├── TechMart.FinanceAgent/      # Remote A2A agent
│   └── TechMart.SupportAgent/      # Remote A2A agent
└── README.md
```

## Key Technologies

- **.NET 10** - Target framework
- **.NET Aspire 13.0.0** - Service orchestration and observability
- **Microsoft Agent Framework** - Core agent infrastructure
- **A2A Protocol** - Agent-to-Agent communication
- **Azure OpenAI** - LLM backend (gpt-4o)
- **OpenTelemetry** - Distributed tracing

## Extending the Solution

### Adding a New A2A Agent

1. Create a new ASP.NET Core project
2. Add project references to the agent framework
3. Define tools using `[Description]` attributes
4. Configure AgentCard with capabilities
5. Map A2A endpoint with `app.MapA2A()`
6. Register in AppHost

### Adding Workflows

Define workflows in AgentHost using the fluent builder:

```csharp
builder.AddWorkflow("my-workflow", workflow =>
{
    workflow.SetDescription("My custom workflow");
    workflow.AddStep("step-1", step =>
    {
        step.SetAgentName("my-agent");
        step.SetDescription("First step description");
    });
});
```

## Contributing

See the main [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
