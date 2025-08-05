using A2A;

namespace A2ACustomerService.Services;

public static class AgentCardFactory
{
    public static AgentCard CreateFrontDeskCard(string agentUrl) => new AgentCard
    {
        Name = "Front Desk Agent",
        Description = "Handles general customer inquiries and support.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "acknowledge_request" },
            new AgentSkill { Name = "route_to_specialist" },
            new AgentSkill { Name = "customer_greeting" }
        }
    };

    public static AgentCard CreateBillingCard(string agentUrl) => new AgentCard
    {
        Name = "Billing Agent",
        Description = "Handles billing and payment-related inquiries.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "process_refund" },
            new AgentSkill { Name = "explain_charges" },
            new AgentSkill { Name = "resolve_payment_issue" },
            new AgentSkill { Name = "account_inquiry" }
        }
    };

    public static AgentCard CreateTechnicalCard(string agentUrl) => new AgentCard
    {
        Name = "Technical Agent",
        Description = "Handles technical support and troubleshooting.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "diagnose_issue" },
            new AgentSkill { Name = "troubleshoot" },
            new AgentSkill { Name = "provide_guidance" },
            new AgentSkill { Name = "system_explanation" }
        }
    };

    public static AgentCard CreateOrchestratorCard(string agentUrl) => new AgentCard
    {
        Name = "Orchestrator Agent",
        Description = "Coordinates multi-agent workflows and escalations.",
        Url = agentUrl,
        Version = "1.0.0",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Capabilities = new AgentCapabilities { Streaming = true },
        Skills = new List<AgentSkill>
        {
            new AgentSkill { Name = "synthesize_responses" },
            new AgentSkill { Name = "coordinate_agents" },
            new AgentSkill { Name = "escalate_ticket" },
            new AgentSkill { Name = "finalize_response" }
        }
    };
}
