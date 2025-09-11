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
            Type = "tech",
            Status = AgentStatus.Idle,
            Description = "Resolves technical problems",
            Capabilities = ["troubleshooting", "technical-support", "system-diagnostics"],
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

    public Task<CustomerTicket> SubmitTicketAsync(SubmitTicketRequest request)
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

        return Task.FromResult(ticket);
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
            // LAYER 1: Front Desk Agent - Customer Interaction & Routing
            ticket.Status = TicketStatus.Routing;
            _agents["front-desk"].Status = AgentStatus.Processing;
            _agents["front-desk"].CurrentTicket = ticketId;

            await Task.Delay(1200); // Simulate A2A communication latency

            // Front desk agent acknowledges and determines routing
            var assignedAgents = DetermineAssignedAgents(ticket);
            Console.WriteLine($"[MOCK] Assigned agents for ticket {ticketId}: [{string.Join(", ", assignedAgents)}]");
            ticket.AssignedAgents = assignedAgents;

            var frontDeskResponse = new AgentResponse
            {
                AgentId = "front-desk",
                AgentType = "front-desk",
                Response = $"Thank you {ticket.CustomerName}. I've received your inquiry regarding \"{ticket.Subject}\" and I'm coordinating with our specialist team to assist you.",
                Status = ResponseStatus.Completed,
                Timestamp = DateTime.UtcNow
            };

            // LAYER 2: Specialist Agents - Domain Expert Processing
            ticket.Status = TicketStatus.Processing;
            _agents["front-desk"].Status = AgentStatus.Completed;

            var specialistResponses = new List<AgentResponse>();

            // Process with specialized agents using mock A2A protocol
            foreach (var agentId in assignedAgents)
            {
                if (_agents.TryGetValue(agentId, out var agent))
                {
                    agent.Status = AgentStatus.Processing;
                    agent.CurrentTicket = ticketId;

                    await Task.Delay(1800); // Simulate specialist processing time

                    var mockResponse = GenerateEnhancedMockResponse(ticket, agentId);
                    specialistResponses.Add(mockResponse);

                    agent.Status = AgentStatus.Completed;
                    agent.CurrentTicket = null;
                }
            }

            // LAYER 3: Orchestrator Agent - Response Synthesis & Coordination
            if (specialistResponses.Count > 0)
            {
                Console.WriteLine($"[MOCK] Starting orchestrator for ticket {ticketId} with {specialistResponses.Count} specialist responses");
                _agents["orchestrator"].Status = AgentStatus.Processing;
                _agents["orchestrator"].CurrentTicket = ticketId;

                await Task.Delay(1500); // Simulate orchestration processing time

                // Orchestrator synthesizes multiple specialist responses
                var synthesizedResponse = GenerateMockOrchestratorResponse(ticket, specialistResponses);

                // Create final customer response
                ticket.FinalResponse = GenerateMockFinalCustomerResponse(ticket, synthesizedResponse);

                // Store all responses including the synthesized one
                var allResponses = new List<AgentResponse> { frontDeskResponse };
                allResponses.AddRange(specialistResponses);
                allResponses.Add(synthesizedResponse);
                ticket.Responses = allResponses;

                _agents["orchestrator"].Status = AgentStatus.Completed;
                _agents["orchestrator"].CurrentTicket = null;
                Console.WriteLine($"[MOCK] Orchestrator completed for ticket {ticketId}");
            }
            else
            {
                Console.WriteLine($"[MOCK] No orchestration needed for ticket {ticketId} - single agent scenario");
                // Single agent scenario - no orchestration needed
                ticket.FinalResponse = GenerateSimpleMockFinalResponse(ticket, frontDeskResponse);
                ticket.Responses = [frontDeskResponse];
            }

            ticket.Status = TicketStatus.Completed;

            // Reset all agents to idle state
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

        // Enhanced scenarios for orchestrator demonstration - check these FIRST
        // These scenarios will automatically trigger multi-agent responses for better orchestrator showcasing
        if (text.Contains("login") && (text.Contains("refund") || text.Contains("billing") || text.Contains("payment")))
        {
            // Login issues affecting billing access - needs both agents
            return ["billing", "technical"];
        }
        else if (text.Contains("account") && (text.Contains("access") || text.Contains("error")))
        {
            // Account access issues often involve both billing and technical aspects
            return ["billing", "technical"];
        }
        else if (text.Contains("system") && text.Contains("charge"))
        {
            // System charging issues need both technical diagnosis and billing correction
            return ["billing", "technical"];
        }
        else if (text.Contains("update") || text.Contains("upgrade"))
        {
            // Updates often affect both technical functionality and billing
            return ["billing", "technical"];
        }
        else if (text.Contains("broken") && text.Contains("access"))
        {
            // Broken access issues - both technical and billing
            return ["billing", "technical"];
        }
        else if (text.Contains("connectivity") && (text.Contains("charge") || text.Contains("bill")))
        {
            // Connectivity issues affecting billing
            return ["billing", "technical"];
        }

        // Check for billing-related keywords
        if (text.Contains("billing") || text.Contains("payment") || text.Contains("charge") ||
            text.Contains("refund") || text.Contains("invoice") || ticket.Category == "billing")
        {
            agents.Add("billing");
        }

        // Check for technical keywords
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

        return agents.Distinct().ToList(); // Remove duplicates
    }

    private AgentResponse GenerateEnhancedMockResponse(CustomerTicket ticket, string agentId)
    {
        // Enhanced responses that will work well with orchestrator synthesis
        var responses = agentId switch
        {
            "billing" => new[]
            {
                $"I've reviewed {ticket.CustomerName}'s billing inquiry and processed a refund of $45.99 for the unauthorized charge. The refund will appear in 3-5 business days, and I've added account monitoring to prevent future issues.",
                $"I've investigated the payment discrepancy for {ticket.CustomerName} and found a system error that duplicated the monthly charge. A credit of $29.99 has been applied, and the billing cycle has been corrected.",
                $"After reviewing {ticket.CustomerName}'s account, I've identified incorrect billing tier assignment. I've updated the account to the correct plan, resulting in a $15.00 monthly reduction going forward."
            },
            "technical" => new[]
            {
                $"I've diagnosed {ticket.CustomerName}'s login issue and found expired authentication tokens. I've reset the credentials and implemented two-factor authentication for enhanced security. Login should work immediately.",
                $"The technical problem reported by {ticket.CustomerName} was caused by a recent system update. I've deployed a targeted fix and verified the functionality is restored. All features should now work correctly.",
                $"I've identified a compatibility issue affecting {ticket.CustomerName}'s browser configuration. I've provided custom browser settings and updated their user profile to prevent similar issues."
            },
            _ => new[] { $"I've reviewed {ticket.CustomerName}'s request and provided comprehensive assistance tailored to their specific needs." }
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

    private AgentResponse GenerateMockOrchestratorResponse(CustomerTicket ticket, List<AgentResponse> specialistResponses)
    {
        // Create a realistic mock scenario - let's make a combined login + refund issue
        var combinedScenarios = new[]
        {
            // Scenario 1: Login issue preventing access to billing
            $"After coordinating with our billing and technical specialists, I've identified that {ticket.CustomerName} was unable to process their refund request due to login authentication issues. Our technical team has resolved the login problem, and our billing team has now successfully processed the refund. This comprehensive solution addresses both the immediate access issue and the underlying billing concern.",
            
            // Scenario 2: Billing error affecting account access
            $"Our team coordination reveals that {ticket.CustomerName}'s technical difficulties were actually caused by billing system conflicts. The billing team has corrected account inconsistencies, while the technical team has synchronized the user profile. This integrated approach ensures both billing accuracy and system functionality are restored.",
            
            // Scenario 3: System update affecting both billing and login
            $"Through A2A coordination, we've determined that {ticket.CustomerName}'s issues stem from a recent system update that affected both billing calculations and login protocols. Our technical team has implemented compatibility fixes, while our billing team has corrected any calculation errors. This unified response ensures seamless service restoration."
        };

        return new AgentResponse
        {
            AgentId = "orchestrator",
            AgentType = "orchestrator",
            Response = combinedScenarios[_random.Next(combinedScenarios.Length)],
            Status = ResponseStatus.Completed,
            Timestamp = DateTime.UtcNow
        };
    }

    private string GenerateMockFinalCustomerResponse(CustomerTicket ticket, AgentResponse synthesizedResponse)
    {
        return $"Dear {ticket.CustomerName},\n\n" +
               $"Thank you for contacting our customer service team regarding \"{ticket.Subject}\".\n\n" +
               $"{synthesizedResponse.Response}\n\n" +
               $"Our coordinated response ensures that all aspects of your inquiry have been thoroughly addressed by our specialized team members working together. " +
               $"If you need any further assistance or have additional questions, please don't hesitate to contact us.\n\n" +
               $"Best regards,\nCustomer Service Team";
    }

    private string GenerateSimpleMockFinalResponse(CustomerTicket ticket, AgentResponse frontDeskResponse)
    {
        return $"Dear {ticket.CustomerName},\n\n" +
               $"Thank you for contacting our customer service team regarding \"{ticket.Subject}\".\n\n" +
               $"{frontDeskResponse.Response}\n\n" +
               $"Our team has reviewed your inquiry and provided assistance with your {ticket.Category} matter. " +
               $"If you need any further clarification or have additional questions, please don't hesitate to reach out.\n\n" +
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
