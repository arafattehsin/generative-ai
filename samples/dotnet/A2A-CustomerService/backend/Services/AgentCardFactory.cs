using A2A;

namespace A2ACustomerService.Services;

public static class AgentCardFactory
{
    public static AgentCard CreateFrontDeskCard(string agentUrl) => new AgentCard
    {
        Name = "Front Desk Agent",
        Description = "Handles general customer inquiries, acknowledges requests, and routes to appropriate specialists. Provides brief, professional responses.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true, PushNotifications = false },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "acknowledge_request", Description = "Acknowledge customer requests professionally." },
            new AgentSkill { Name = "route_to_specialist", Description = "Route requests to the appropriate specialist agent." },
            new AgentSkill { Name = "customer_greeting", Description = "Greet customers and confirm receipt of inquiry." }
        }

    };

    public static AgentCard CreateBillingCard(string agentUrl) => new AgentCard
    {
        Name = "Billing Agent",
        Description = "Handles billing, payment, and account-related inquiries. Explains charges, processes refunds, and resolves payment issues.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true, PushNotifications = false },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "process_refund", Description = "Process refunds or adjustments for customers." },
            new AgentSkill { Name = "explain_charges", Description = "Explain charges and account information clearly." },
            new AgentSkill { Name = "resolve_payment_issue", Description = "Resolve payment and billing issues." },
            new AgentSkill { Name = "account_inquiry", Description = "Handle account-related questions and requests." }
        }
    };

    public static AgentCard CreateTechnicalCard(string agentUrl) => new AgentCard
    {
        Name = "Technical Agent",
        Description = "Provides technical support, troubleshooting, and step-by-step guidance. Explains technical concepts in simple terms.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true, PushNotifications = false },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "diagnose_issue", Description = "Diagnose and resolve technical issues." },
            new AgentSkill { Name = "troubleshoot", Description = "Provide step-by-step troubleshooting guidance." },
            new AgentSkill { Name = "provide_guidance", Description = "Offer clear technical solutions and advice." },
            new AgentSkill { Name = "system_explanation", Description = "Explain system features and usage in simple terms." }
        },

    };

    public static AgentCard CreateOrchestratorCard(string agentUrl) => new AgentCard
    {
        Name = "Orchestrator Agent",
        Description = "Synthesizes responses from multiple specialist agents, coordinates workflows, and escalates tickets as needed.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true, PushNotifications = false },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "synthesize_responses", Description = "Combine and summarize responses from multiple agents." },
            new AgentSkill { Name = "coordinate_agents", Description = "Coordinate actions and workflows between agents." },
            new AgentSkill { Name = "escalate_ticket", Description = "Escalate tickets to higher-level support when needed." },
            new AgentSkill { Name = "finalize_response", Description = "Create the final customer-facing response." }
        }

    };
}
