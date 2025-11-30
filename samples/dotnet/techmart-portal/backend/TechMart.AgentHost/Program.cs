// Copyright (c) Microsoft. All rights reserved.

// TechMart Enterprise Operations Portal - Agent Host
// Main service hosting coordinator agents, workflows, and DevUI
// Integrates with remote A2A agents (Finance, Inventory, Support)

using System.ClientModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using A2A;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════
// Aspire Service Defaults
// ═══════════════════════════════════════════════════════════════════

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// ═══════════════════════════════════════════════════════════════════
// CORS for React Frontend
// ═══════════════════════════════════════════════════════════════════

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ═══════════════════════════════════════════════════════════════════
// AG-UI Service Registration
// ═══════════════════════════════════════════════════════════════════

builder.Services.AddAGUI();

// Add named HttpClients for A2A agents - will configure URLs after build
builder.Services.AddHttpClient("finance-agent");
builder.Services.AddHttpClient("inventory-agent");
builder.Services.AddHttpClient("support-agent");

// ═══════════════════════════════════════════════════════════════════
// Azure OpenAI Configuration
// ═══════════════════════════════════════════════════════════════════

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
builder.Services.AddChatClient(chatClient);

// Connect to A2A agents and get their function tools
async Task<List<AIFunction>> GetA2AAgentTools(string agentUrl, string agentName)
{
    var tools = new List<AIFunction>();
    try
    {
        Console.WriteLine($"   Connecting to {agentName} at {agentUrl}...");

        var uri = new Uri(agentUrl);
        var httpClient = new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(30) };

        // Use the correct agent card path - the A2A agents expose at /.well-known/agent-card.json
        var cardResolver = new A2ACardResolver(uri, httpClient, agentCardPath: "/.well-known/agent-card.json");
        var agentCard = await cardResolver.GetAgentCardAsync();
        var a2aAgent = await cardResolver.GetAIAgentAsync();

        foreach (var skill in agentCard.Skills ?? [])
        {
            var options = new AIFunctionFactoryOptions
            {
                Name = SanitizeFunctionName($"{agentName}_{skill.Name}"),
                Description = $"""
                    A2A Agent: {agentCard.Name}
                    Skill: {skill.Name}
                    Description: {skill.Description}
                    Tags: [{string.Join(", ", skill.Tags ?? [])}]
                    """
            };

            tools.Add(AIFunctionFactory.Create(
                async (string input, CancellationToken ct) =>
                {
                    var response = await a2aAgent.RunAsync(input, cancellationToken: ct);
                    return response.Text;
                },
                options));
        }
        Console.WriteLine($"   ✅ Connected to {agentName}: {tools.Count} skills");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ⚠️  {agentName} not available: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"      Inner: {ex.InnerException.Message}");
        }
    }
    return tools;
}

static string SanitizeFunctionName(string name) =>
    Regex.Replace(name, "[^0-9A-Za-z_]", "_").ToLower();

// ═══════════════════════════════════════════════════════════════════
// TechMart Retail Data (Simulated)
// ═══════════════════════════════════════════════════════════════════

Dictionary<string, (string Status, string Items, decimal Total, string Customer)> orders = new()
{
    ["ORD-2024-001"] = ("Processing", "Gaming Laptop ROG Strix x2", 3799.98m, "Contoso Corp"),
    ["ORD-2024-002"] = ("Shipped", "Bulk Monitors x50", 27499.50m, "Fabrikam Inc"),
    ["ORD-2024-003"] = ("Pending Approval", "Server Rack + Accessories", 15999.00m, "Northwind Traders"),
    ["ORD-2024-004"] = ("Delivered", "Office Keyboards x100", 4999.00m, "Adventure Works"),
    ["ORD-2024-005"] = ("Awaiting Payment", "Enterprise SSD Bundle", 8999.99m, "Contoso Corp"),
};

Dictionary<string, (int Stock, int Reserved, string Warehouse)> inventory = new()
{
    ["Gaming Laptop"] = (150, 25, "Seattle-WH1"),
    ["4K Monitor"] = (500, 120, "Seattle-WH1"),
    ["Mechanical Keyboard"] = (1200, 300, "Portland-WH2"),
    ["Enterprise SSD"] = (80, 15, "Seattle-WH1"),
    ["Server Rack"] = (12, 3, "Portland-WH2"),
    ["USB-C Hub"] = (800, 50, "Seattle-WH1"),
};

Dictionary<string, (string Tier, decimal CreditLimit, decimal Balance)> customers = new()
{
    ["Contoso Corp"] = ("Enterprise", 100000m, 45000m),
    ["Fabrikam Inc"] = ("Premium", 50000m, 28000m),
    ["Northwind Traders"] = ("Standard", 25000m, 15999m),
    ["Adventure Works"] = ("Enterprise", 100000m, 12000m),
};

// ═══════════════════════════════════════════════════════════════════
// Tool Functions
// ═══════════════════════════════════════════════════════════════════

[Description("Look up an enterprise order by order ID")]
string LookupOrder([Description("The order ID (e.g., ORD-2024-001)")] string orderId)
{
    if (orders.TryGetValue(orderId.ToUpper(), out var order))
    {
        return $"Order {orderId}: Status={order.Status}, Items={order.Items}, Total=${order.Total:N2}, Customer={order.Customer}";
    }
    return $"Order {orderId} not found.";
}

[Description("Get customer account information including credit status")]
string GetCustomerAccount([Description("Customer name to look up")] string customerName)
{
    var match = customers.FirstOrDefault(c =>
        c.Key.Contains(customerName, StringComparison.OrdinalIgnoreCase));
    if (match.Key != null)
    {
        decimal available = match.Value.CreditLimit - match.Value.Balance;
        return $"{match.Key}: Tier={match.Value.Tier}, Credit Limit=${match.Value.CreditLimit:N2}, " +
               $"Current Balance=${match.Value.Balance:N2}, Available Credit=${available:N2}";
    }
    return $"Customer '{customerName}' not found.";
}

[Description("Check inventory levels for a product")]
string CheckStock([Description("Product name to check")] string productName)
{
    var match = inventory.FirstOrDefault(p =>
        p.Key.Contains(productName, StringComparison.OrdinalIgnoreCase));
    if (match.Key != null)
    {
        int available = match.Value.Stock - match.Value.Reserved;
        string status = available switch
        {
            < 10 => "LOW STOCK",
            < 50 => "MODERATE",
            _ => "IN STOCK"
        };
        return $"{match.Key}: {available} available ({match.Value.Stock} total, {match.Value.Reserved} reserved) at {match.Value.Warehouse} [{status}]";
    }
    return $"Product '{productName}' not found.";
}

[Description("List all pending orders requiring action")]
string GetPendingOrders()
{
    var pending = orders.Where(o =>
        o.Value.Status.Contains("Pending") ||
        o.Value.Status.Contains("Awaiting")).ToList();

    if (pending.Count == 0) return "No pending orders.";

    return "Pending Orders:\n" + string.Join("\n", pending.Select(o =>
        $"  - {o.Key}: {o.Value.Status} - {o.Value.Items} (${o.Value.Total:N2}) for {o.Value.Customer}"));
}

// ═══════════════════════════════════════════════════════════════════
// Register Agents with Hosting Framework
// ═══════════════════════════════════════════════════════════════════

// Operations Coordinator - main orchestrator
IHostedAgentBuilder coordinatorBuilder = builder.AddAIAgent("operations-coordinator",
    instructions: """
        You are TechMart's Enterprise Operations Coordinator. You orchestrate retail operations
        for B2B customers including:
        - Order management and status tracking
        - Customer account inquiries
        - Inventory availability checks
        - Routing complex requests to specialized teams
        
        You have access to order lookup, customer accounts, and inventory tools.
        Be professional, efficient, and proactive in identifying issues.
        """,
    description: "Main operations coordinator for enterprise retail",
    chatClientServiceKey: null)
    .WithAITools(
        AIFunctionFactory.Create(LookupOrder),
        AIFunctionFactory.Create(GetCustomerAccount),
        AIFunctionFactory.Create(CheckStock),
        AIFunctionFactory.Create(GetPendingOrders)
    )
    .WithInMemoryThreadStore();

// Order Specialist - handles complex order operations
IHostedAgentBuilder orderSpecialistBuilder = builder.AddAIAgent("order-specialist",
    instructions: """
        You are TechMart's Order Specialist. You handle complex order operations:
        - Order modifications and cancellations
        - Rush order processing
        - Bulk order coordination
        - Delivery scheduling
        
        Always verify order details before making changes.
        """,
    description: "Specialist for complex order operations",
    chatClientServiceKey: null)
    .WithAITools(
        AIFunctionFactory.Create(LookupOrder),
        AIFunctionFactory.Create(CheckStock)
    );

// Account Manager - handles customer relationships
IHostedAgentBuilder accountManagerBuilder = builder.AddAIAgent("account-manager",
    instructions: """
        You are TechMart's Enterprise Account Manager. You manage B2B customer relationships:
        - Account reviews and credit assessments
        - Contract negotiations
        - Volume discount discussions
        - Relationship management
        
        Focus on customer satisfaction and long-term partnerships.
        """,
    description: "Enterprise account relationship manager",
    chatClientServiceKey: null)
    .WithAITools(
        AIFunctionFactory.Create(GetCustomerAccount),
        AIFunctionFactory.Create(LookupOrder)
    );

// ═══════════════════════════════════════════════════════════════════
// Register Workflows
// ═══════════════════════════════════════════════════════════════════

// Order Fulfillment Workflow: Coordinator → Order Specialist → Account Manager
builder.AddWorkflow("order-fulfillment", (sp, key) =>
{
    var agents = new List<IHostedAgentBuilder>
    {
        coordinatorBuilder,
        orderSpecialistBuilder,
        accountManagerBuilder
    }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));

    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
}).AddAsAIAgent();

// Parallel Review Workflow: All specialists review simultaneously
builder.AddWorkflow("parallel-review", (sp, key) =>
{
    var agents = new List<IHostedAgentBuilder>
    {
        orderSpecialistBuilder,
        accountManagerBuilder
    }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));

    return AgentWorkflowBuilder.BuildConcurrent(workflowName: key, agents: agents);
}).AddAsAIAgent();

// ═══════════════════════════════════════════════════════════════════
// Add OpenAI Responses and Conversations for DevUI
// ═══════════════════════════════════════════════════════════════════

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// ═══════════════════════════════════════════════════════════════════
// Build Application
// ═══════════════════════════════════════════════════════════════════

var app = builder.Build();

// Enable CORS for React frontend
app.UseCors("AllowFrontend");

app.MapOpenApi();

// ═══════════════════════════════════════════════════════════════════
// Initialize Tavily MCP for Web Search
// ═══════════════════════════════════════════════════════════════════

IList<AITool> tavilyTools = [];
try
{
    var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
    {
        Name = "TavilySearch",
        Command = "npx",
        Arguments = ["-y", "@tavily/mcp-server"],
        EnvironmentVariables = new Dictionary<string, string?>
        {
            ["TAVILY_API_KEY"] = Environment.GetEnvironmentVariable("TAVILY_API_KEY") ?? ""
        }
    }));
    var mcpTools = await mcpClient.ListToolsAsync();
    tavilyTools = mcpTools.Cast<AITool>().ToList();
    Console.WriteLine($"✅ Tavily MCP initialized with {tavilyTools.Count} tools");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Tavily MCP not available: {ex.Message}");
}

// ═══════════════════════════════════════════════════════════════════
// Connect to Remote A2A Agents
// ═══════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine("🔗 Connecting to A2A Agents...");

// Get URLs from Aspire service discovery (injected via WithReference)
// Aspire injects as: services__<name>__http__0 or ConnectionStrings__<name>
string GetServiceUrl(string serviceName, string fallbackPort)
{
    // Try different Aspire formats
    var url = app.Configuration[$"services:{serviceName}:http:0"]
        ?? app.Configuration[$"services:{serviceName}:https:0"]
        ?? app.Configuration.GetConnectionString(serviceName)
        ?? $"http://localhost:{fallbackPort}";
    return url;
}

// Ports based on launchSettings.json: Inventory=5101, Finance=5102, Support=5103
var financeUrl = GetServiceUrl("finance-agent", "5102");
var inventoryUrl = GetServiceUrl("inventory-agent", "5101");
var supportUrl = GetServiceUrl("support-agent", "5103");

Console.WriteLine($"   Finance URL:   {financeUrl}");
Console.WriteLine($"   Inventory URL: {inventoryUrl}");
Console.WriteLine($"   Support URL:   {supportUrl}");

var a2aTools = new List<AIFunction>();

// Connect to Finance Agent
a2aTools.AddRange(await GetA2AAgentTools(financeUrl, "finance"));

// Connect to Inventory Agent  
a2aTools.AddRange(await GetA2AAgentTools(inventoryUrl, "inventory"));

// Connect to Support Agent
a2aTools.AddRange(await GetA2AAgentTools(supportUrl, "support"));

Console.WriteLine($"   Total A2A skills available: {a2aTools.Count}");

// Get the base coordinator agent
var baseCoordinator = app.Services.GetRequiredKeyedService<AIAgent>("operations-coordinator");

// Create enhanced coordinator with A2A tools for AG-UI
var allTools = new List<AITool>();
allTools.AddRange(a2aTools);
allTools.AddRange(tavilyTools);
// Add the local tools
allTools.Add(AIFunctionFactory.Create(LookupOrder));
allTools.Add(AIFunctionFactory.Create(GetCustomerAccount));
allTools.Add(AIFunctionFactory.Create(CheckStock));
allTools.Add(AIFunctionFactory.Create(GetPendingOrders));

var coordinatorAgent = chatClient.CreateAIAgent(
    name: "TechMart Operations Coordinator",
    instructions: """
        You are TechMart's Enterprise Operations Coordinator - the central hub for B2B retail operations.
        
        **Your Core Capabilities:**
        1. Order Management: Lookup orders, check status, identify pending orders
        2. Customer Accounts: Review account info, credit status, tier levels
        3. Inventory: Check stock levels across warehouses
        
        **Specialized A2A Agents Available:**
        - FINANCE Agent: Credit checks, pricing calculations, quotes, payment processing
        - INVENTORY Agent: Stock transfers, reservations, warehouse operations  
        - SUPPORT Agent: Ticket creation, escalations, customer issue tracking
        
        When users ask about pricing, credit, payments → use finance_ tools
        When users ask about stock, warehouses, reservations → use inventory_ tools
        When users report issues, need support → use support_ tools
        
        **IMPORTANT - Response Attribution:**
        When you use a tool or consult another agent, ALWAYS clearly indicate this in your response:
        - Start with "[Operations Coordinator]" for general responses
        - When using Finance agent tools, say "📊 [Consulting Finance Agent]" before sharing the results
        - When using Inventory agent tools, say "📦 [Consulting Inventory Agent]" before sharing the results  
        - When using Support agent tools, say "🎫 [Consulting Support Agent]" before sharing the results
        - When using web search (tavily), say "🔍 [Web Search]" before sharing results
        - When using local tools (LookupOrder, GetCustomerAccount, etc.), prefix with "📋 [Checking Internal Systems]"
        
        This helps users understand which systems and agents are being consulted for their request.
        
        Be professional, efficient, and proactive. Always confirm actions before executing.
        """,
    tools: allTools
);

// ═══════════════════════════════════════════════════════════════════
// Map Endpoints
// ═══════════════════════════════════════════════════════════════════

// OpenAI-compatible endpoints for DevUI
app.MapOpenAIResponses();
app.MapOpenAIConversations();

// DevUI for interactive testing
app.MapDevUI();

// AG-UI endpoint for CopilotKit frontend
app.MapAGUI("/agui", coordinatorAgent);

// Health endpoints
app.MapDefaultEndpoints();

// ═══════════════════════════════════════════════════════════════════
// Startup Banner
// ═══════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       TechMart Enterprise Operations Portal - Agent Host         ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("📋 Local Agents:");
Console.WriteLine("   • operations-coordinator - Main orchestrator with A2A routing");
Console.WriteLine("   • order-specialist       - Complex order operations");
Console.WriteLine("   • account-manager        - B2B relationship management");
Console.WriteLine();
Console.WriteLine("🔗 Remote A2A Agents:");
Console.WriteLine("   • finance-agent          - http://finance-agent (via Aspire)");
Console.WriteLine("   • inventory-agent        - http://inventory-agent (via Aspire)");
Console.WriteLine("   • support-agent          - http://support-agent (via Aspire)");
Console.WriteLine();
Console.WriteLine("🔄 Workflows:");
Console.WriteLine("   • order-fulfillment      - Sequential: Coord → Order → Account");
Console.WriteLine("   • parallel-review        - Concurrent specialist review");
Console.WriteLine();
Console.WriteLine("🌐 Endpoints:");
Console.WriteLine("   • /devui                 - Interactive testing UI");
Console.WriteLine("   • /agui                  - AG-UI for CopilotKit frontend");
Console.WriteLine("   • /openapi               - API documentation");
Console.WriteLine();
Console.WriteLine("🔍 Integrations:");
Console.WriteLine($"   • Tavily MCP             - {(tavilyTools.Count > 0 ? "✅ Web search available" : "⚠️ Not configured")}");
Console.WriteLine($"   • A2A Agents             - {a2aTools.Count} skills loaded");
Console.WriteLine();

app.Run();
