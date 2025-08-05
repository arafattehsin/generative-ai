
using A2A;
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

        var responseText = await _llmService.GenerateTextAsync(prompt, CancellationToken.None);

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
As a front desk agent, your role is ONLY to:
1. Acknowledge the customer's request professionally
2. Confirm that you've received their inquiry
3. Inform them that you're coordinating with the appropriate specialist team
4. Keep it brief and professional

DO NOT:

Keep your response to 1-2 sentences maximum. Simply acknowledge and confirm routing to specialists.
";

        return await CreateResponseAsync(ticket, specificPrompt);
    }

    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = async (messageSendParams, ct) =>
        {
            var ticket = CustomerTicket.FromMessage(messageSendParams.Message);
            var response = await ProcessTicketAsync(ticket);
            return response.ToMessage();
        };
        taskManager.OnAgentCardQuery = async (agentUrl, ct) => AgentCardFactory.CreateFrontDeskCard(agentUrl);
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

    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = async (messageSendParams, ct) =>
        {
            var ticket = CustomerTicket.FromMessage(messageSendParams.Message);
            var response = await ProcessTicketAsync(ticket);
            return response.ToMessage();
        };
        taskManager.OnAgentCardQuery = async (agentUrl, ct) => AgentCardFactory.CreateBillingCard(agentUrl);
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

    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = async (messageSendParams, ct) =>
        {
            var ticket = CustomerTicket.FromMessage(messageSendParams.Message);
            var response = await ProcessTicketAsync(ticket);
            return response.ToMessage();
        };
        taskManager.OnAgentCardQuery = async (agentUrl, ct) => AgentCardFactory.CreateTechnicalCard(agentUrl);
    }
}

public class OrchestratorAgent : BaseA2AAgent
{
    public OrchestratorAgent(ILLMService llmService)
        : base(llmService, "orchestrator", "Response Orchestrator Agent")
    {
    }

    public override async Task<AgentResponse> ProcessTicketAsync(CustomerTicket ticket)
    {
        // This method won't be used for the orchestrator as it handles multiple responses
        throw new NotImplementedException("Use SynthesizeResponsesAsync for orchestrator agent");
    }

    public async Task<AgentResponse> SynthesizeResponsesAsync(CustomerTicket ticket, List<AgentResponse> specialistResponses)
    {
        var responseDetails = string.Join("\n", specialistResponses.Select(r =>
            $"- {r.AgentType} Agent Response: {r.Response}"));

        var specificPrompt = $@"
As a Response Orchestrator Agent using A2A protocol, your role is to:

1. ANALYZE multiple specialist agent responses for a customer service ticket
2. SYNTHESIZE them into a single, coherent, customer-friendly response
3. ENSURE no important information is lost or duplicated
4. MAINTAIN professional customer service tone
5. RESOLVE any conflicting information between agents

CUSTOMER TICKET:
Subject: {ticket.Subject}
Description: {ticket.Description}
Category: {ticket.Category}
Priority: {ticket.Priority}

SPECIALIST AGENT RESPONSES TO SYNTHESIZE:
{responseDetails}

ORCHESTRATION RULES:

Create a synthesized response that reads as if it came from a single, knowledgeable customer service representative who consulted with various departments. The summary should not exceed more than 30-50 words.
";

        var synthesizedText = await _llmService.GenerateTextAsync(specificPrompt, CancellationToken.None);

        return new AgentResponse
        {
            AgentId = "orchestrator",
            AgentType = "orchestrator",
            Response = synthesizedText,
            Status = ResponseStatus.Completed,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<string> CreateFinalCustomerResponseAsync(CustomerTicket ticket, AgentResponse synthesizedResponse)
    {
        var finalPrompt = $@"
As a Response Orchestrator Agent, create the final customer communication.

CUSTOMER DETAILS:
Name: {ticket.CustomerName}
Subject: {ticket.Subject}

SYNTHESIZED RESPONSE FROM SPECIALISTS:
{synthesizedResponse.Response}

FORMAT REQUIREMENTS:

Create the final response that will be sent to the customer.
";

        return await _llmService.GenerateTextAsync(finalPrompt, CancellationToken.None);
    }

    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = async (messageSendParams, ct) =>
        {
            // Orchestrator agent expects a list of specialist responses, so this is a placeholder
            return new Message { Role = MessageRole.Agent, Parts = new List<Part> { new TextPart { Text = "Orchestrator response not implemented." } } };
        };
        taskManager.OnAgentCardQuery = async (agentUrl, ct) => AgentCardFactory.CreateOrchestratorCard(agentUrl);
    }
}
