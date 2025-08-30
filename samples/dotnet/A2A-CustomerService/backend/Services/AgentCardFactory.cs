using A2A;

namespace A2ACustomerService.Services;

public static class AgentCardFactory
{
    public static AgentCard CreateFrontDeskCard(string agentUrl) => new AgentCard
    {
        ProtocolVersion = "0.3.0",
        Name = "Front Desk Agent",
        Description = "Handles general customer inquiries, acknowledges requests, and routes to appropriate specialists. Provides brief, professional responses.",
        Url = agentUrl,
        DocumentationUrl = "https://github.com/arafattehsin/generative-ai/blob/main/samples/dotnet/A2A-CustomerService/README.md#front-desk-agent",
        PreferredTransport = AgentTransport.JsonRpc,
        AdditionalInterfaces = new List<AgentInterface>
        {
            new AgentInterface { Url = agentUrl, Transport = AgentTransport.JsonRpc },
            new AgentInterface { Url = agentUrl + "/v1", Transport = new AgentTransport("HTTP+JSON") }
        },
        Provider = new AgentProvider
        {
            Organization = "A2A Customer Service",
            Url = "https://github.com/arafattehsin/generative-ai"
        },
        Version = "1.0.0",
        DefaultInputModes = ["text/plain"],
        DefaultOutputModes = ["text/plain"],
        Capabilities = new AgentCapabilities { Streaming = false, PushNotifications = false },
        SecuritySchemes = new Dictionary<string, SecurityScheme>(),
        Skills = new List<AgentSkill>
        {
            new AgentSkill
            {
                Id = "acknowledge_request",
                Name = "acknowledge_request",
                Description = "Acknowledge customer requests professionally.",
                Tags = ["customer-service", "routing"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "route_to_specialist",
                Name = "route_to_specialist",
                Description = "Route requests to the appropriate specialist agent.",
                Tags = ["routing", "coordination"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "customer_greeting",
                Name = "customer_greeting",
                Description = "Greet customers and confirm receipt of inquiry.",
                Tags = ["customer-service", "communication"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            }
        }

    };

    public static AgentCard CreateBillingCard(string agentUrl) => new AgentCard
    {
        ProtocolVersion = "0.3.0",
        Name = "Billing Agent",
        Description = "Handles billing, payment, and account-related inquiries. Explains charges, processes refunds, and resolves payment issues.",
        Url = agentUrl,
        DocumentationUrl = "https://github.com/arafattehsin/generative-ai/blob/main/samples/dotnet/A2A-CustomerService/README.md#billing-agent",
        PreferredTransport = AgentTransport.JsonRpc,
        AdditionalInterfaces = new List<AgentInterface>
        {
            new AgentInterface { Url = agentUrl, Transport = AgentTransport.JsonRpc },
            new AgentInterface { Url = agentUrl + "/v1", Transport = new AgentTransport("HTTP+JSON") }
        },
        Provider = new AgentProvider
        {
            Organization = "A2A Customer Service",
            Url = "https://github.com/arafattehsin/generative-ai"
        },
        Version = "1.0.0",
        DefaultInputModes = ["text/plain"],
        DefaultOutputModes = ["text/plain"],
        Capabilities = new AgentCapabilities { Streaming = false, PushNotifications = false },
        SecuritySchemes = new Dictionary<string, SecurityScheme>(),
        Skills = new List<AgentSkill>
        {
            new AgentSkill
            {
                Id = "process_refund",
                Name = "process_refund",
                Description = "Process refunds or adjustments for customers.",
                Tags = ["billing", "refund", "payment"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "explain_charges",
                Name = "explain_charges",
                Description = "Explain charges and account information clearly.",
                Tags = ["billing", "account", "information"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "resolve_payment_issue",
                Name = "resolve_payment_issue",
                Description = "Resolve payment and billing issues.",
                Tags = ["billing", "payment", "support"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "account_inquiry",
                Name = "account_inquiry",
                Description = "Handle account-related questions and requests.",
                Tags = ["account", "inquiry", "support"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            }
        }
    };

    public static AgentCard CreateTechnicalCard(string agentUrl) => new AgentCard
    {
        ProtocolVersion = "0.3.0",
        Name = "Technical Agent",
        Description = "Provides technical support, troubleshooting, and step-by-step guidance. Explains technical concepts in simple terms.",
        Url = agentUrl,
        DocumentationUrl = "https://github.com/arafattehsin/generative-ai/blob/main/samples/dotnet/A2A-CustomerService/README.md#technical-agent",
        PreferredTransport = AgentTransport.JsonRpc,
        AdditionalInterfaces = new List<AgentInterface>
        {
            new AgentInterface { Url = agentUrl, Transport = AgentTransport.JsonRpc },
            new AgentInterface { Url = agentUrl + "/v1", Transport = new AgentTransport("HTTP+JSON") }
        },
        Provider = new AgentProvider
        {
            Organization = "A2A Customer Service",
            Url = "https://github.com/arafattehsin/generative-ai"
        },
        Version = "1.0.0",
        DefaultInputModes = ["text/plain"],
        DefaultOutputModes = ["text/plain"],
        Capabilities = new AgentCapabilities { Streaming = false, PushNotifications = false },
        SecuritySchemes = new Dictionary<string, SecurityScheme>(),
        Skills = new List<AgentSkill>
        {
            new AgentSkill
            {
                Id = "diagnose_issue",
                Name = "diagnose_issue",
                Description = "Diagnose and resolve technical issues.",
                Tags = ["technical", "diagnosis", "troubleshooting"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "troubleshoot",
                Name = "troubleshoot",
                Description = "Provide step-by-step troubleshooting guidance.",
                Tags = ["technical", "troubleshooting", "guidance"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "provide_guidance",
                Name = "provide_guidance",
                Description = "Offer clear technical solutions and advice.",
                Tags = ["technical", "guidance", "solutions"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "system_explanation",
                Name = "system_explanation",
                Description = "Explain system features and usage in simple terms.",
                Tags = ["technical", "explanation", "documentation"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            }
        }
    };

    public static AgentCard CreateOrchestratorCard(string agentUrl) => new AgentCard
    {
        ProtocolVersion = "0.3.0",
        Name = "Orchestrator Agent",
        Description = "Synthesizes responses from multiple specialist agents, coordinates workflows, and escalates tickets as needed.",
        Url = agentUrl,
        DocumentationUrl = "https://github.com/arafattehsin/generative-ai/blob/main/samples/dotnet/A2A-CustomerService/README.md#orchestrator-agent",
        PreferredTransport = AgentTransport.JsonRpc,
        AdditionalInterfaces = new List<AgentInterface>
        {
            new AgentInterface { Url = agentUrl, Transport = AgentTransport.JsonRpc },
            new AgentInterface { Url = agentUrl + "/v1", Transport = new AgentTransport("HTTP+JSON") }
        },
        Provider = new AgentProvider
        {
            Organization = "A2A Customer Service",
            Url = "https://github.com/arafattehsin/generative-ai"
        },
        Version = "1.0.0",
        DefaultInputModes = ["text/plain"],
        DefaultOutputModes = ["text/plain"],
        Capabilities = new AgentCapabilities { Streaming = false, PushNotifications = false },
        SecuritySchemes = new Dictionary<string, SecurityScheme>(),
        Skills = new List<AgentSkill>
        {
            new AgentSkill
            {
                Id = "synthesize_responses",
                Name = "synthesize_responses",
                Description = "Combine and summarize responses from multiple agents.",
                Tags = ["orchestration", "synthesis", "coordination"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "coordinate_agents",
                Name = "coordinate_agents",
                Description = "Coordinate actions and workflows between agents.",
                Tags = ["orchestration", "coordination", "workflow"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "escalate_ticket",
                Name = "escalate_ticket",
                Description = "Escalate tickets to higher-level support when needed.",
                Tags = ["escalation", "support", "routing"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            },
            new AgentSkill
            {
                Id = "finalize_response",
                Name = "finalize_response",
                Description = "Create the final customer-facing response.",
                Tags = ["orchestration", "finalization", "customer-service"],
                InputModes = ["text/plain"],
                OutputModes = ["text/plain"]
            }
        }
    };
}
