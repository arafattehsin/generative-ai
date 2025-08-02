using A2ACustomerService.Models;
using Microsoft.AspNetCore.SignalR;

namespace A2ACustomerService.Services;

public class A2ATicketService : ITicketService
{
    private readonly Dictionary<string, CustomerTicket> _tickets = new();
    private readonly Dictionary<string, AgentInfo> _agents = new();
    private readonly FrontDeskAgent _frontDeskAgent;
    private readonly BillingAgent _billingAgent;
    private readonly TechnicalAgent _technicalAgent;
    private readonly IHubContext<Controllers.CustomerServiceHub>? _hubContext;
    private readonly ILogger<A2ATicketService> _logger;

    public A2ATicketService(
        FrontDeskAgent frontDeskAgent,
        BillingAgent billingAgent,
        TechnicalAgent technicalAgent,
        IHubContext<Controllers.CustomerServiceHub>? hubContext,
        ILogger<A2ATicketService> logger)
    {
        _frontDeskAgent = frontDeskAgent;
        _billingAgent = billingAgent;
        _technicalAgent = technicalAgent;
        _hubContext = hubContext;
        _logger = logger;

        InitializeAgents();
    }

    private void InitializeAgents()
    {
        _agents["front-desk"] = new AgentInfo
        {
            Id = "front-desk",
            Name = "Front Desk Agent",
            Type = "front-desk",
            Status = AgentStatus.Idle,
            Description = "Routes tickets and coordinates responses using A2A protocol",
            Capabilities = ["routing", "coordination", "customer-interaction", "a2a-protocol"],
            IconName = "User",
            Color = "text-blue-600"
        };

        _agents["billing"] = new AgentInfo
        {
            Id = "billing",
            Name = "Billing Agent",
            Type = "billing",
            Status = AgentStatus.Idle,
            Description = "Handles billing and account issues using A2A protocol",
            Capabilities = ["billing", "payments", "refunds", "account-management", "a2a-protocol"],
            IconName = "CreditCard",
            Color = "text-green-600"
        };

        _agents["technical"] = new AgentInfo
        {
            Id = "technical",
            Name = "Technical Support Agent",
            Type = "tech",
            Status = AgentStatus.Idle,
            Description = "Resolves technical problems using A2A protocol",
            Capabilities = ["troubleshooting", "technical-support", "system-diagnostics", "a2a-protocol"],
            IconName = "Wrench",
            Color = "text-purple-600"
        };
    }

    public async Task<CustomerTicket> SubmitTicketAsync(SubmitTicketRequest request)
    {
        _logger.LogInformation("Processing ticket submission using A2A protocol for customer: {CustomerName}", request.CustomerName);

        var ticket = new CustomerTicket
        {
            Id = Guid.NewGuid().ToString(),
            CustomerName = request.CustomerName,
            Email = request.Email,
            Subject = request.Subject,
            Description = request.Description,
            Category = request.Category,
            Status = TicketStatus.New,
            Priority = DeterminePriority(request.Subject, request.Description),
            Timestamp = DateTime.UtcNow
        };

        _tickets[ticket.Id] = ticket;

        // Start A2A processing asynchronously
        _ = Task.Run(() => ProcessTicketWithA2AAsync(ticket.Id));

        return ticket;
    }

    private TicketPriority DeterminePriority(string subject, string description)
    {
        var text = $"{subject} {description}".ToLower();

        if (text.Contains("urgent") || text.Contains("critical") || text.Contains("down"))
            return TicketPriority.Urgent;
        if (text.Contains("important") || text.Contains("asap"))
            return TicketPriority.High;
        if (text.Contains("minor") || text.Contains("question"))
            return TicketPriority.Low;

        return TicketPriority.Medium;
    }

    private async Task ProcessTicketWithA2AAsync(string ticketId)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket)) return;

        try
        {
            _logger.LogInformation("Starting A2A processing for ticket: {TicketId}", ticketId);

            // Step 1: Front desk agent initiates A2A protocol
            ticket.Status = TicketStatus.Routing;
            _agents["front-desk"].Status = AgentStatus.Processing;
            _agents["front-desk"].CurrentTicket = ticketId;

            await NotifyUpdate(ticket);
            await Task.Delay(1000); // Simulate A2A communication latency

            // Front desk agent processes and determines routing
            var frontDeskResponse = await _frontDeskAgent.ProcessTicketAsync(ticket);
            var responses = new List<AgentResponse> { frontDeskResponse };

            // Determine which agents to involve in A2A protocol
            var assignedAgents = DetermineAssignedAgents(ticket);
            ticket.AssignedAgents = assignedAgents;

            // Step 2: A2A agent-to-agent communication
            ticket.Status = TicketStatus.Processing;
            _agents["front-desk"].Status = AgentStatus.Completed;

            await NotifyUpdate(ticket);

            // Process with specialized agents using A2A protocol
            foreach (var agentId in assignedAgents)
            {
                if (_agents.TryGetValue(agentId, out var agent))
                {
                    agent.Status = AgentStatus.Processing;
                    agent.CurrentTicket = ticketId;
                    await NotifyUpdate(ticket);

                    _logger.LogInformation("A2A communication: {AgentId} processing ticket {TicketId}", agentId, ticketId);

                    // Simulate A2A protocol communication delay
                    await Task.Delay(1500);

                    var agentResponse = agentId switch
                    {
                        "billing" => await _billingAgent.ProcessTicketAsync(ticket),
                        "technical" => await _technicalAgent.ProcessTicketAsync(ticket),
                        _ => frontDeskResponse
                    };

                    responses.Add(agentResponse);

                    agent.Status = AgentStatus.Completed;
                    agent.CurrentTicket = null;
                    await NotifyUpdate(ticket);
                }
            }

            ticket.Responses = responses;

            // Step 3: Generate coordinated final response
            await Task.Delay(800);
            ticket.FinalResponse = GenerateFinalResponse(ticket);
            ticket.Status = TicketStatus.Completed;

            // Reset all agents
            foreach (var agent in _agents.Values)
            {
                agent.Status = AgentStatus.Idle;
                agent.CurrentTicket = null;
            }

            await NotifyUpdate(ticket);
            _logger.LogInformation("A2A processing completed for ticket: {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in A2A processing for ticket: {TicketId}", ticketId);
            ticket.Status = TicketStatus.Failed;
            foreach (var agent in _agents.Values)
            {
                agent.Status = AgentStatus.Idle;
                agent.CurrentTicket = null;
            }
            await NotifyUpdate(ticket);
        }
    }

    private List<string> DetermineAssignedAgents(CustomerTicket ticket)
    {
        var agents = new List<string>();
        var text = $"{ticket.Subject} {ticket.Description} {ticket.Category}".ToLower();

        if (text.Contains("billing") || text.Contains("payment") || text.Contains("charge") ||
            text.Contains("refund") || text.Contains("invoice") || ticket.Category == "billing")
        {
            agents.Add("billing");
        }

        if (text.Contains("technical") || text.Contains("error") || text.Contains("bug") ||
            text.Contains("login") || text.Contains("password") || ticket.Category == "technical")
        {
            agents.Add("technical");
        }

        return agents;
    }

    private string GenerateFinalResponse(CustomerTicket ticket)
    {
        var hasSpecializedResponses = ticket.Responses.Any(r => r.AgentId != "front-desk");

        if (!hasSpecializedResponses)
        {
            return $"Dear {ticket.CustomerName},\n\n" +
                   $"Thank you for contacting our customer service team regarding \"{ticket.Subject}\".\n\n" +
                   $"Our team has reviewed your inquiry using our advanced A2A coordination system and provided assistance with your {ticket.Category} matter. " +
                   $"If you need any further clarification or have additional questions, please don't hesitate to reach out.\n\n" +
                   $"Best regards,\nCustomer Service Team";
        }

        var specializedResponses = ticket.Responses.Where(r => r.AgentId != "front-desk").ToList();
        var responseText = string.Join("\n\n", specializedResponses.Select(r => $"Our {r.AgentType} specialist coordinated through our A2A system: {r.Response}"));

        return $"Dear {ticket.CustomerName},\n\n" +
               $"Thank you for contacting our customer service team regarding \"{ticket.Subject}\".\n\n" +
               $"Our agents have coordinated through our Agent-to-Agent (A2A) communication system to provide you with the best possible assistance:\n\n" +
               $"{responseText}\n\n" +
               $"This coordinated response ensures that all aspects of your inquiry have been addressed by our specialized team members. " +
               $"If you need any further assistance, please don't hesitate to contact us.\n\n" +
               $"Best regards,\nCustomer Service Team";
    }

    private async Task NotifyUpdate(CustomerTicket ticket)
    {
        if (_hubContext != null)
        {
            await _hubContext.Clients.All.SendAsync("TicketUpdated", ticket);
            await _hubContext.Clients.All.SendAsync("AgentsUpdated", _agents.Values.ToList());
        }
    }

    public async Task<CustomerTicket?> GetTicketAsync(string ticketId)
    {
        _tickets.TryGetValue(ticketId, out var ticket);
        return await Task.FromResult(ticket);
    }

    public async Task<List<CustomerTicket>> GetAllTicketsAsync()
    {
        return await Task.FromResult(_tickets.Values.ToList());
    }

    public async Task<List<AgentInfo>> GetAgentsAsync()
    {
        return await Task.FromResult(_agents.Values.ToList());
    }
}
