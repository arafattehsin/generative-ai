using A2ACustomerService.Models;

namespace A2ACustomerService.Services;

public class MockTicketService : ITicketService
{
    private readonly Dictionary<string, CustomerTicket> _tickets = new();
    private readonly Dictionary<string, AgentInfo> _agents = new();
    private readonly Random _random = new();

    public MockTicketService()
    {
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
            Description = "Routes tickets and coordinates responses",
            Capabilities = ["routing", "coordination", "customer-interaction"],
            IconName = "User",
            Color = "text-blue-600"
        };

        _agents["billing"] = new AgentInfo
        {
            Id = "billing",
            Name = "Billing Agent",
            Type = "billing",
            Status = AgentStatus.Idle,
            Description = "Handles billing and account issues",
            Capabilities = ["billing", "payments", "refunds", "account-management"],
            IconName = "CreditCard",
            Color = "text-green-600"
        };

        _agents["technical"] = new AgentInfo
        {
            Id = "technical",
            Name = "Technical Support Agent",
            Type = "technical",
            Status = AgentStatus.Idle,
            Description = "Resolves technical problems",
            Capabilities = ["troubleshooting", "technical-support", "system-diagnostics"],
            IconName = "Wrench",
            Color = "text-purple-600"
        };
    }

    public async Task<CustomerTicket> SubmitTicketAsync(SubmitTicketRequest request)
    {
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

        // Start processing asynchronously
        _ = Task.Run(() => ProcessTicketAsync(ticket.Id));

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

    private async Task ProcessTicketAsync(string ticketId)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket)) return;

        try
        {
            // Step 1: Front desk routing
            ticket.Status = TicketStatus.Routing;
            _agents["front-desk"].Status = AgentStatus.Processing;
            _agents["front-desk"].CurrentTicket = ticketId;

            await Task.Delay(2000); // Simulate processing time

            // Determine which agents to assign
            var assignedAgents = DetermineAssignedAgents(ticket);
            ticket.AssignedAgents = assignedAgents;

            // Step 2: Process with assigned agents
            ticket.Status = TicketStatus.Processing;
            _agents["front-desk"].Status = AgentStatus.Completed;

            foreach (var agentId in assignedAgents)
            {
                if (_agents.TryGetValue(agentId, out var agent))
                {
                    agent.Status = AgentStatus.Processing;
                    agent.CurrentTicket = ticketId;
                }
            }

            await Task.Delay(1500); // Simulate agent processing

            // Generate responses
            var responses = new List<AgentResponse>();

            // Front desk response
            responses.Add(new AgentResponse
            {
                AgentId = "front-desk",
                AgentType = "front-desk",
                Response = $"Thank you {ticket.CustomerName}. I've reviewed your {ticket.Category} request and coordinated with our specialized team.",
                Status = ResponseStatus.Completed,
                Timestamp = DateTime.UtcNow
            });

            // Specialized agent responses
            foreach (var agentId in assignedAgents)
            {
                responses.Add(GenerateMockResponse(ticket, agentId));

                if (_agents.TryGetValue(agentId, out var agent))
                {
                    agent.Status = AgentStatus.Completed;
                    agent.CurrentTicket = null;
                }
            }

            ticket.Responses = responses;

            // Step 3: Generate final response
            await Task.Delay(1000);
            ticket.FinalResponse = GenerateFinalResponse(ticket);
            ticket.Status = TicketStatus.Completed;

            // Reset all agents
            foreach (var agent in _agents.Values)
            {
                agent.Status = AgentStatus.Idle;
                agent.CurrentTicket = null;
            }
        }
        catch (Exception)
        {
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

        // If no specific agent is needed, assign to front-desk only
        if (agents.Count == 0)
        {
            return new List<string>(); // Front desk will handle alone
        }

        return agents;
    }

    private AgentResponse GenerateMockResponse(CustomerTicket ticket, string agentId)
    {
        var responses = agentId switch
        {
            "billing" => new[]
            {
                "I've reviewed your billing inquiry and found the issue. A refund has been processed and will appear in 3-5 business days.",
                "Your account has been updated with the correct charges. I've also added a credit for the inconvenience.",
                "I've investigated the payment discrepancy and corrected your account balance. You should see the changes reflected immediately."
            },
            "technical" => new[]
            {
                "I've diagnosed the technical issue and implemented a fix. Please try the solution and let me know if you need further assistance.",
                "The login problem has been resolved by resetting your authentication tokens. You should be able to access your account now.",
                "I've identified the bug in the system and deployed a patch. The issue should be resolved within the next hour."
            },
            _ => new[] { "I've reviewed your request and provided the appropriate assistance." }
        };

        return new AgentResponse
        {
            AgentId = agentId,
            AgentType = agentId,
            Response = responses[_random.Next(responses.Length)],
            Status = ResponseStatus.Completed,
            Timestamp = DateTime.UtcNow
        };
    }

    private string GenerateFinalResponse(CustomerTicket ticket)
    {
        var hasSpecializedResponses = ticket.Responses.Any(r => r.AgentId != "front-desk");

        if (!hasSpecializedResponses)
        {
            return $"Dear {ticket.CustomerName},\n\n" +
                   $"Thank you for contacting our customer service team regarding \"{ticket.Subject}\".\n\n" +
                   $"I have reviewed your inquiry and provided assistance with your {ticket.Category} matter. " +
                   $"If you need any further clarification or have additional questions, please don't hesitate to reach out.\n\n" +
                   $"Best regards,\nCustomer Service Team";
        }

        var specializedResponses = ticket.Responses.Where(r => r.AgentId != "front-desk").ToList();
        var responseText = string.Join("\n\n", specializedResponses.Select(r => $"Our {r.AgentType} specialist has addressed your concern: {r.Response}"));

        return $"Dear {ticket.CustomerName},\n\n" +
               $"Thank you for contacting our customer service team regarding \"{ticket.Subject}\".\n\n" +
               $"{responseText}\n\n" +
               $"We believe this resolves your inquiry. If you need any further assistance, please don't hesitate to contact us.\n\n" +
               $"Best regards,\nCustomer Service Team";
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
