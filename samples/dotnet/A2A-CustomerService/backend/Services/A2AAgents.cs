using A2ACustomerService.Models;

namespace A2ACustomerService.Services;

public abstract class BaseA2AAgent
{
    protected readonly ILLMService _llmService;
    protected readonly string _agentType;
    protected readonly string _agentName;

    protected BaseA2AAgent(ILLMService llmService, string agentType, string agentName)
    {
        _llmService = llmService;
        _agentType = agentType;
        _agentName = agentName;
    }

    public abstract Task<AgentResponse> ProcessTicketAsync(CustomerTicket ticket);

    protected async Task<AgentResponse> CreateResponseAsync(CustomerTicket ticket, string specificPrompt)
    {
        var prompt = $@"
You are a {_agentName} in a customer service department.
Customer: {ticket.CustomerName}
Subject: {ticket.Subject}
Description: {ticket.Description}
Category: {ticket.Category}
Priority: {ticket.Priority}

{specificPrompt}

Provide a professional, helpful response that addresses the customer's specific concern.
Be empathetic and solution-oriented.
";

        var responseText = await _llmService.GenerateResponseAsync(prompt);

        return new AgentResponse
        {
            AgentId = _agentType,
            AgentType = _agentType,
            Response = responseText,
            Status = ResponseStatus.Completed,
            Timestamp = DateTime.UtcNow
        };
    }
}

public class FrontDeskAgent : BaseA2AAgent
{
    public FrontDeskAgent(ILLMService llmService)
        : base(llmService, "front-desk", "Front Desk Agent")
    {
    }

    public override async Task<AgentResponse> ProcessTicketAsync(CustomerTicket ticket)
    {
        var specificPrompt = @"
As a front desk agent, your role is to:
1. Acknowledge the customer's request
2. Coordinate with appropriate specialists if needed
3. Provide initial guidance and set expectations

Focus on being welcoming and ensuring the customer feels heard.
";

        return await CreateResponseAsync(ticket, specificPrompt);
    }
}

public class BillingAgent : BaseA2AAgent
{
    public BillingAgent(ILLMService llmService)
        : base(llmService, "billing", "Billing Specialist")
    {
    }

    public override async Task<AgentResponse> ProcessTicketAsync(CustomerTicket ticket)
    {
        var specificPrompt = @"
As a billing specialist, your role is to:
1. Address billing inquiries, payment issues, and account concerns
2. Explain charges and provide account information
3. Process refunds or adjustments when appropriate
4. Ensure billing accuracy and customer satisfaction

Focus on being clear about financial matters and resolving billing issues.
";

        return await CreateResponseAsync(ticket, specificPrompt);
    }
}

public class TechnicalAgent : BaseA2AAgent
{
    public TechnicalAgent(ILLMService llmService)
        : base(llmService, "technical", "Technical Support Specialist")
    {
    }

    public override async Task<AgentResponse> ProcessTicketAsync(CustomerTicket ticket)
    {
        var specificPrompt = @"
As a technical support specialist, your role is to:
1. Diagnose and resolve technical issues
2. Provide step-by-step troubleshooting guidance
3. Explain technical concepts in simple terms
4. Ensure the customer can successfully use the system

Focus on being thorough and providing clear technical solutions.
";

        return await CreateResponseAsync(ticket, specificPrompt);
    }
}
