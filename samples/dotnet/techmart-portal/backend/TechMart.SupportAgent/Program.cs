// Copyright (c) Microsoft. All rights reserved.

// TechMart Enterprise Operations Portal - Support Agent
// Remote A2A agent for customer support operations

using System.ClientModel;
using System.ComponentModel;
using A2A;
using A2A.AspNetCore;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure Azure OpenAI from connection string
string connectionString = builder.Configuration.GetConnectionString("chat-model")
    ?? throw new InvalidOperationException("chat-model connection string not configured");

var connectionParts = connectionString.Split(';')
    .Select(p => p.Split('=', 2))
    .Where(p => p.Length == 2)
    .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

string endpoint = connectionParts.TryGetValue("Endpoint", out string? e) ? e : throw new InvalidOperationException("Endpoint not found");
string deployment = connectionParts.TryGetValue("Deployment", out string? d) ? d : throw new InvalidOperationException("Deployment not found");

AzureOpenAIClient aoaiClient = connectionParts.TryGetValue("Key", out string? key) && !string.IsNullOrEmpty(key)
    ? new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
    : new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());

IChatClient chatClient = aoaiClient.GetChatClient(deployment).AsIChatClient();

// Simulated support tickets
Dictionary<string, (string CustomerId, string Subject, string Priority, string Status, string Category, DateTime Created, List<string> Notes)> tickets = new()
{
    ["TKT-2024-001"] = ("CUST-001", "Laptop not powering on", "High", "Open", "Hardware", DateTime.Now.AddDays(-2), ["Power button unresponsive", "Tried different outlet"]),
    ["TKT-2024-002"] = ("CUST-002", "Software license activation failed", "Medium", "In Progress", "Software", DateTime.Now.AddDays(-1), ["Error code: 0x80070005"]),
    ["TKT-2024-003"] = ("CUST-004", "Request for bulk order discount", "Low", "Pending", "Sales", DateTime.Now.AddHours(-12), ["Interested in 50 units"]),
    ["TKT-2024-004"] = ("CUST-003", "Delivery damaged - missing parts", "Critical", "Escalated", "Shipping", DateTime.Now.AddDays(-3), ["Package visibly damaged", "3 items missing"])
};

// Knowledge base articles
Dictionary<string, (string Title, string Category, List<string> Steps)> knowledgeBase = new()
{
    ["KB-001"] = ("How to reset your TechMart laptop", "Hardware", ["Hold power button for 10 seconds", "Disconnect all peripherals", "Remove battery if possible", "Wait 30 seconds", "Reconnect and power on"]),
    ["KB-002"] = ("License activation troubleshooting", "Software", ["Ensure internet connectivity", "Run as administrator", "Check firewall settings", "Verify license key format"]),
    ["KB-003"] = ("Return and refund policy", "Policy", ["30-day return window", "Original packaging required", "Restocking fee may apply", "Refunds in 5-7 business days"]),
    ["KB-004"] = ("Shipping and delivery FAQ", "Shipping", ["Standard: 5-7 business days", "Express: 2-3 business days", "Track at techmart.com/track"])
};

int ticketCounter = 5;

// Support tool functions
[Description("Create a new support ticket for a customer issue")]
string CreateTicket([Description("Customer ID")] string customerId, [Description("Issue subject")] string subject, [Description("Priority: Critical, High, Medium, Low")] string priority, [Description("Category: Hardware, Software, Shipping, Sales, Policy")] string category, [Description("Issue description")] string description)
{
    string ticketId = $"TKT-2024-{ticketCounter++:D3}";
    tickets[ticketId] = (customerId, subject, priority, "Open", category, DateTime.Now, [description]);

    return $"""
        Ticket Created: {ticketId}
        - Customer: {customerId}
        - Subject: {subject}
        - Priority: {priority}
        - Category: {category}
        - Status: Open
        - Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
        - Message: A support representative will contact you shortly.
        """;
}

[Description("Get the current status and details of a support ticket")]
string GetTicketStatus([Description("Ticket ID (e.g., TKT-2024-001)")] string ticketId)
{
    if (tickets.TryGetValue(ticketId, out var ticket))
    {
        int ageHours = (int)(DateTime.Now - ticket.Created).TotalHours;
        return $"""
            Ticket: {ticketId}
            - Customer: {ticket.CustomerId}
            - Subject: {ticket.Subject}
            - Priority: {ticket.Priority}
            - Status: {ticket.Status}
            - Category: {ticket.Category}
            - Created: {ticket.Created:yyyy-MM-dd HH:mm:ss}
            - Age: {ageHours} hours
            - Notes: {string.Join(" | ", ticket.Notes)}
            """;
    }
    return $"Ticket {ticketId} not found.";
}

[Description("Update the status and add a note to an existing ticket")]
string UpdateTicket([Description("Ticket ID")] string ticketId, [Description("New status: Open, In Progress, Pending, Resolved, Closed, Escalated")] string status, [Description("Note to add")] string note)
{
    if (tickets.TryGetValue(ticketId, out var ticket))
    {
        var updatedNotes = new List<string>(ticket.Notes) { $"[{DateTime.Now:HH:mm}] {note}" };
        tickets[ticketId] = (ticket.CustomerId, ticket.Subject, ticket.Priority, status, ticket.Category, ticket.Created, updatedNotes);

        return $"""
            Ticket Updated: {ticketId}
            - New Status: {status}
            - Note Added: {note}
            - Updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            """;
    }
    return $"Ticket {ticketId} not found.";
}

[Description("Escalate a ticket to a higher level of support")]
string EscalateTicket([Description("Ticket ID")] string ticketId, [Description("Escalate to: Technical Lead, Product Manager, Engineering, Legal, Executive")] string escalateTo, [Description("Reason for escalation")] string reason)
{
    if (tickets.TryGetValue(ticketId, out var ticket))
    {
        var updatedNotes = new List<string>(ticket.Notes) { $"[{DateTime.Now:HH:mm}] ESCALATED to {escalateTo}: {reason}" };
        tickets[ticketId] = (ticket.CustomerId, ticket.Subject, ticket.Priority, "Escalated", ticket.Category, ticket.Created, updatedNotes);

        string escalationId = $"ESC-{Random.Shared.Next(100, 999)}";
        return $"""
            Ticket Escalated: {ticketId}
            - Escalation ID: {escalationId}
            - Escalated To: {escalateTo}
            - Priority: {ticket.Priority}
            - Reason: {reason}
            - Expected Response: Within 2 hours for {ticket.Priority} priority
            """;
    }
    return $"Ticket {ticketId} not found.";
}

[Description("Search the knowledge base for articles matching the query")]
string SearchKnowledgeBase([Description("Search query")] string query)
{
    string queryLower = query.ToLower();
    var matchingArticles = knowledgeBase.Where(kb =>
        kb.Value.Title.ToLower().Contains(queryLower) ||
        kb.Value.Category.ToLower().Contains(queryLower) ||
        kb.Value.Steps.Any(s => s.ToLower().Contains(queryLower)))
        .ToList();

    if (matchingArticles.Count > 0)
    {
        return "Knowledge Base Results:\n" + string.Join("\n\n", matchingArticles.Select(kb =>
            $"[{kb.Key}] {kb.Value.Title} ({kb.Value.Category})\nSteps: {string.Join("; ", kb.Value.Steps)}"));
    }

    return "No matching articles found. Consider creating a support ticket for personalized assistance.";
}

[Description("Get all support tickets for a specific customer")]
string GetCustomerTickets([Description("Customer ID")] string customerId)
{
    var customerTickets = tickets.Where(t => t.Value.CustomerId == customerId)
        .OrderByDescending(t => t.Value.Created)
        .ToList();

    if (customerTickets.Count == 0)
        return $"No tickets found for customer {customerId}.";

    int openCount = customerTickets.Count(t => t.Value.Status is not "Resolved" and not "Closed");

    return $"""
        Tickets for {customerId}: {customerTickets.Count} total, {openCount} open
        {string.Join("\n", customerTickets.Select(t =>
            $"  - {t.Key}: {t.Value.Subject} [{t.Value.Priority}] - {t.Value.Status}"))}
        """;
}

// Create the Support Agent
AIAgent supportAgent = chatClient.CreateAIAgent(
    name: "SupportAgent",
    instructions: """
        You are the TechMart Support Agent, responsible for customer support operations.
        
        Your capabilities include:
        1. Ticket Management: Create, update, and track support tickets
        2. Escalations: Escalate critical issues to appropriate teams
        3. Knowledge Base: Search for solutions in the knowledge base
        4. Customer History: View customer's ticket history
        
        Priority Guidelines:
        - Critical: System down, data loss, security issues - respond within 1 hour
        - High: Major functionality impaired - respond within 4 hours
        - Medium: Minor issues with workarounds - respond within 24 hours
        - Low: Questions, feature requests - respond within 48 hours
        
        Always search the knowledge base first before escalating.
        Provide empathetic and professional responses.
        """,
    tools: [
        AIFunctionFactory.Create(CreateTicket),
        AIFunctionFactory.Create(GetTicketStatus),
        AIFunctionFactory.Create(UpdateTicket),
        AIFunctionFactory.Create(EscalateTicket),
        AIFunctionFactory.Create(SearchKnowledgeBase),
        AIFunctionFactory.Create(GetCustomerTickets)
    ]);

// Expose as A2A agent
AgentCard supportAgentCard = new()
{
    Name = "TechMart Support Agent",
    Description = "Handles customer support tickets, escalations, knowledge base search, and support queue management for TechMart retail operations",
    Version = "1.0.0",
    Url = "/", // Relative URL - actual host URL is determined by Aspire
    Capabilities = new() { Streaming = true },
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Skills =
    [
        new AgentSkill { Id = "ticket-management", Name = "Ticket Management", Description = "Create, update, and track customer support tickets" },
        new AgentSkill { Id = "escalations", Name = "Escalation Management", Description = "Escalate issues to Technical Lead, Product Manager, Engineering, Legal, or Executive" },
        new AgentSkill { Id = "knowledge-base", Name = "Knowledge Base Search", Description = "Search for solutions and troubleshooting guides" },
        new AgentSkill { Id = "customer-history", Name = "Customer History", Description = "View customer's ticket history and support interactions" }
    ]
};

app.MapA2A(supportAgent, "/", supportAgentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/"));

app.Run();
