using A2ACustomerService.Models;

namespace A2ACustomerService.Services;

public class A2ATicketService : ITicketService
{
    private readonly Dictionary<string, CustomerTicket> _tickets = new();
    private readonly Dictionary<string, AgentInfo> _agents = new();
    private readonly FrontDeskAgent _frontDeskAgent;
    private readonly BillingAgent _billingAgent;
    private readonly TechnicalAgent _technicalAgent;
    private readonly OrchestratorAgent _orchestratorAgent;
    private readonly ILogger<A2ATicketService> _logger;

    public A2ATicketService(
        FrontDeskAgent frontDeskAgent,
        BillingAgent billingAgent,
        TechnicalAgent technicalAgent,
        OrchestratorAgent orchestratorAgent,
        ILogger<A2ATicketService> logger)
    {
        _frontDeskAgent = frontDeskAgent;
        _billingAgent = billingAgent;
        _technicalAgent = technicalAgent;
        _orchestratorAgent = orchestratorAgent;
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
            Type = "technical",
            Status = AgentStatus.Idle,
            Description = "Resolves technical problems using A2A protocol",
            Capabilities = ["troubleshooting", "technical-support", "system-diagnostics", "a2a-protocol"],
            IconName = "Wrench",
            Color = "text-purple-600"
        };

        _agents["orchestrator"] = new AgentInfo
        {
            Id = "orchestrator",
            Name = "Response Orchestrator Agent",
            Type = "orchestrator",
            Status = AgentStatus.Idle,
            Description = "Synthesizes and coordinates multi-agent responses using A2A protocol",
            Capabilities = ["response-synthesis", "quality-assurance", "coordination", "conflict-resolution", "a2a-protocol"],
            IconName = "ArrowsClockwise",
            Color = "text-orange-600"
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
            _logger.LogInformation("Starting A2A Three-Layer Processing for ticket: {TicketId}", ticketId);

            // LAYER 1: Front Desk Agent - Customer Interaction & Routing
            ticket.Status = TicketStatus.Routing;
            _agents["front-desk"].Status = AgentStatus.Processing;
            _agents["front-desk"].CurrentTicket = ticketId;

            await Task.Delay(1000); // Simulate A2A communication latency

            // Front desk agent acknowledges and determines routing
            var frontDeskResponse = await _frontDeskAgent.ProcessTicketAsync(ticket);
            var assignedAgents = DetermineAssignedAgents(ticket);
            ticket.AssignedAgents = assignedAgents;

            _logger.LogInformation("A2A Layer 1 Complete: Front Desk routed to {AgentCount} specialists", assignedAgents.Count);

            // LAYER 2: Specialist Agents - Domain Expert Processing
            ticket.Status = TicketStatus.Processing;
            _agents["front-desk"].Status = AgentStatus.Completed;

            var specialistResponses = new List<AgentResponse>();

            // Process with specialized agents using A2A protocol
            foreach (var agentId in assignedAgents)
            {
                if (_agents.TryGetValue(agentId, out var agent))
                {
                    agent.Status = AgentStatus.Processing;
                    agent.CurrentTicket = ticketId;

                    _logger.LogInformation("A2A Layer 2: {AgentId} processing ticket {TicketId}", agentId, ticketId);

                    // Simulate A2A protocol communication delay
                    await Task.Delay(1500);

                    var agentResponse = agentId switch
                    {
                        "billing" => await _billingAgent.ProcessTicketAsync(ticket),
                        "technical" => await _technicalAgent.ProcessTicketAsync(ticket),
                        _ => throw new InvalidOperationException($"Unknown agent type: {agentId}")
                    };

                    specialistResponses.Add(agentResponse);

                    agent.Status = AgentStatus.Completed;
                    agent.CurrentTicket = null;
                }
            }

            _logger.LogInformation("A2A Layer 2 Complete: {ResponseCount} specialist responses collected", specialistResponses.Count);

            // LAYER 3: Orchestrator Agent - Response Synthesis & Coordination
            if (specialistResponses.Count > 0)
            {
                _agents["orchestrator"].Status = AgentStatus.Processing;
                _agents["orchestrator"].CurrentTicket = ticketId;

                _logger.LogInformation("A2A Layer 3: Orchestrator synthesizing {ResponseCount} specialist responses", specialistResponses.Count);

                await Task.Delay(1200); // Simulate orchestration processing time

                // Orchestrator synthesizes multiple specialist responses
                var synthesizedResponse = await _orchestratorAgent.SynthesizeResponsesAsync(ticket, specialistResponses);

                // Create final customer response with proper formatting
                ticket.FinalResponse = $"Dear {ticket.CustomerName},\n\n" +
                                     $"Thank you for contacting our customer service team regarding '{ticket.Subject}'.\n\n" +
                                     $"Our A2A agent system has processed your request with specialist coordination.\n\n" +
                                     $"Based on our analysis, here's your complete resolution:\n\n" +
                                     $"{synthesizedResponse.Response}\n\n" +
                                     $"This solution has been coordinated across our specialized departments to ensure all aspects of your concern are addressed.\n\n" +
                                     $"Best regards,\nA2A Customer Service Team";

                _agents["orchestrator"].Status = AgentStatus.Completed;
                _agents["orchestrator"].CurrentTicket = null;

                _logger.LogInformation("A2A Layer 3 Complete: Orchestrator created unified response");
            }
            else
            {
                // Single agent scenario - no orchestration needed
                ticket.FinalResponse = await CreateSimpleFinalResponseAsync(ticket, frontDeskResponse);

                _logger.LogInformation("A2A Processing: Single agent response, no orchestration required");
            }

            ticket.Status = TicketStatus.Completed;

            // Reset all agents to idle state
            foreach (var agent in _agents.Values)
            {
                agent.Status = AgentStatus.Idle;
                agent.CurrentTicket = null;
            }

            _logger.LogInformation("A2A Three-Layer Processing completed for ticket: {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in A2A Three-Layer processing for ticket: {TicketId}", ticketId);
            ticket.Status = TicketStatus.Failed;
            foreach (var agent in _agents.Values)
            {
                agent.Status = AgentStatus.Idle;
                agent.CurrentTicket = null;
            }
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

    private Task<string> CreateSimpleFinalResponseAsync(CustomerTicket ticket, AgentResponse frontDeskResponse)
    {
        // For single-agent scenarios, create a simple professional response
        var response = $"Dear {ticket.CustomerName},\n\n" +
               $"Thank you for contacting our customer service team regarding \"{ticket.Subject}\".\n\n" +
               $"{frontDeskResponse.Response}\n\n" +
               $"Our team has reviewed your inquiry and provided assistance with your {ticket.Category} matter. " +
               $"If you need any further clarification or have additional questions, please don't hesitate to reach out.\n\n" +
               $"Best regards,\nCustomer Service Team";

        return Task.FromResult(response);
    }

    public Task<CustomerTicket?> GetTicketAsync(string ticketId)
    {
        _tickets.TryGetValue(ticketId, out var ticket);
        return Task.FromResult(ticket);
    }

    public Task<List<CustomerTicket>> GetAllTicketsAsync()
    {
        return Task.FromResult(_tickets.Values.ToList());
    }

    public Task<List<AgentInfo>> GetAgentsAsync()
    {
        return Task.FromResult(_agents.Values.ToList());
    }
}
