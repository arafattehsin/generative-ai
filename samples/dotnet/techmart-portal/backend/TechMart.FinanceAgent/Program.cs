// Copyright (c) Microsoft. All rights reserved.

// TechMart Enterprise Operations Portal - Finance Agent
// Remote A2A agent for financial operations

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

// Simulated customer credit data
Dictionary<string, (string Name, decimal CreditLimit, decimal Balance, string Rating, bool NetTerms)> customers = new()
{
    ["CUST-001"] = ("TechMart Premium", 50000m, 12500m, "Excellent", true),
    ["CUST-002"] = ("Acme Corp", 25000m, 22000m, "Good", true),
    ["CUST-003"] = ("StartupXYZ", 10000m, 9800m, "Fair", false),
    ["CUST-004"] = ("Enterprise Ltd", 100000m, 15000m, "Excellent", true),
    ["CUST-005"] = ("NewBiz Inc", 5000m, 4500m, "Limited", false)
};

// Simulated pricing tiers based on annual spend
Dictionary<string, decimal> customerAnnualSpend = new()
{
    ["CUST-001"] = 75000m,
    ["CUST-002"] = 15000m,
    ["CUST-003"] = 3000m,
    ["CUST-004"] = 250000m,
    ["CUST-005"] = 1500m
};

// Finance tool functions
[Description("Check credit status, limit, and available credit for a customer")]
string CheckCredit([Description("Customer ID (e.g., CUST-001)")] string customerId)
{
    if (customers.TryGetValue(customerId, out var info))
    {
        decimal available = info.CreditLimit - info.Balance;
        return $"""
            Customer: {info.Name} ({customerId})
            - Credit Limit: ${info.CreditLimit:N2}
            - Current Balance: ${info.Balance:N2}
            - Available Credit: ${available:N2}
            - Credit Rating: {info.Rating}
            - Net Terms Approved: {(info.NetTerms ? "Yes" : "No")}
            - Status: {(available > 0 ? "Active" : "Credit Limit Reached")}
            """;
    }
    return $"Customer {customerId} not found in credit system.";
}

[Description("Calculate tier-based pricing with customer-specific discounts")]
string CalculatePricing([Description("Customer ID")] string customerId, [Description("Order amount in dollars")] decimal orderAmount)
{
    decimal annualSpend = customerAnnualSpend.GetValueOrDefault(customerId, 0m);

    var (tierName, discountPercent, benefits) = annualSpend switch
    {
        >= 500000m => ("Platinum", 0.20m, "20% discount + dedicated account manager"),
        >= 100000m => ("Gold", 0.15m, "15% discount + priority support"),
        >= 25000m => ("Silver", 0.10m, "10% discount + free shipping"),
        >= 5000m => ("Bronze", 0.05m, "5% discount on orders"),
        _ => ("Standard", 0m, "MSRP pricing")
    };

    decimal discount = orderAmount * discountPercent;
    decimal finalAmount = orderAmount - discount;

    return $"""
        Pricing for {customerId}:
        - Annual Spend: ${annualSpend:N2}
        - Pricing Tier: {tierName}
        - Benefits: {benefits}
        - Original Amount: ${orderAmount:N2}
        - Discount: {discountPercent * 100:N0}% (${discount:N2})
        - Final Amount: ${finalAmount:N2}
        """;
}

[Description("Create a new quote for a customer with automatic tier pricing")]
string CreateQuote([Description("Customer ID")] string customerId, [Description("Description of the quote")] string description, [Description("Quote amount")] decimal amount, [Description("Days until quote expires")] int validDays)
{
    decimal annualSpend = customerAnnualSpend.GetValueOrDefault(customerId, 0m);
    decimal discountPercent = annualSpend switch
    {
        >= 500000m => 0.20m,
        >= 100000m => 0.15m,
        >= 25000m => 0.10m,
        >= 5000m => 0.05m,
        _ => 0m
    };

    string quoteId = $"QT-2024-{Random.Shared.Next(100, 999)}";
    decimal quotedAmount = amount * (1 - discountPercent);
    DateTime expiryDate = DateTime.Now.AddDays(validDays);

    return $"""
        Quote Created: {quoteId}
        - Customer: {customerId}
        - Description: {description}
        - Original Amount: ${amount:N2}
        - Discount Applied: {discountPercent * 100:N0}%
        - Quoted Amount: ${quotedAmount:N2}
        - Valid Until: {expiryDate:yyyy-MM-dd}
        - Status: Pending
        """;
}

[Description("Process a payment for an order")]
string ProcessPayment([Description("Customer ID")] string customerId, [Description("Order ID")] string orderId, [Description("Payment amount")] decimal amount, [Description("Payment method: credit, invoice, prepaid, wire")] string paymentMethod)
{
    bool isApproved = paymentMethod.ToLower() switch
    {
        "credit" => customers.TryGetValue(customerId, out var info) && (info.CreditLimit - info.Balance) >= amount,
        "invoice" => customers.TryGetValue(customerId, out var netInfo) && netInfo.NetTerms,
        "prepaid" or "wire" => true,
        _ => false
    };

    string transactionId = $"TXN-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";

    return $"""
        Payment {(isApproved ? "Approved" : "Declined")}
        - Transaction ID: {transactionId}
        - Order: {orderId}
        - Customer: {customerId}
        - Amount: ${amount:N2}
        - Method: {paymentMethod}
        - Reason: {(isApproved ? "Payment processed successfully" :
                   paymentMethod.ToLower() == "credit" ? "Insufficient credit available" :
                   paymentMethod.ToLower() == "invoice" ? "Customer not approved for net terms" :
                   "Invalid payment method")}
        - Processed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
        """;
}

[Description("Get comprehensive financial summary for a customer")]
string GetFinancialSummary([Description("Customer ID")] string customerId)
{
    if (customers.TryGetValue(customerId, out var info))
    {
        decimal ytdPurchases = customerAnnualSpend.GetValueOrDefault(customerId, 0m) * 0.6m; // Simulated YTD
        int openOrders = Random.Shared.Next(0, 5);

        return $"""
            Financial Summary for {info.Name} ({customerId})
            - Credit Rating: {info.Rating}
            - YTD Purchases: ${ytdPurchases:N2}
            - Current Balance: ${info.Balance:N2}
            - Open Orders: {openOrders}
            - Outstanding Invoices: ${info.Balance:N2}
            - Available Credit: ${info.CreditLimit - info.Balance:N2}
            - Payment Terms: {(info.NetTerms ? "Net 30" : "Prepaid")}
            - Account Status: {(info.Rating is "Excellent" or "Good" ? "Good Standing" : "Review Required")}
            """;
    }
    return $"Customer {customerId} not found.";
}

// Create the Finance Agent
AIAgent financeAgent = chatClient.CreateAIAgent(
    name: "FinanceAgent",
    instructions: """
        You are the TechMart Finance Agent, responsible for all financial operations.
        
        Your capabilities include:
        1. Credit Management: Check customer credit status, limits, and ratings
        2. Pricing & Discounts: Calculate tier-based pricing and discounts
        3. Quotes: Create and track customer quotes with automatic tier pricing
        4. Payment Processing: Process payments via credit, invoice, prepaid, or wire
        5. Financial Reporting: Provide customer financial summaries
        
        Pricing Tiers:
        - Standard: Orders under $5K - MSRP pricing
        - Bronze: $5K-$25K annual spend - 5% discount
        - Silver: $25K-$100K annual spend - 10% discount + free shipping
        - Gold: $100K-$500K annual spend - 15% discount + priority support
        - Platinum: $500K+ annual spend - 20% discount + dedicated account manager
        
        Always verify credit status before approving large orders.
        """,
    tools: [
        AIFunctionFactory.Create(CheckCredit),
        AIFunctionFactory.Create(CalculatePricing),
        AIFunctionFactory.Create(CreateQuote),
        AIFunctionFactory.Create(ProcessPayment),
        AIFunctionFactory.Create(GetFinancialSummary)
    ]);

// Expose as A2A agent
AgentCard financeAgentCard = new()
{
    Name = "TechMart Finance Agent",
    Description = "Handles pricing, quotes, credit checks, payment processing, and financial reporting for TechMart retail operations",
    Version = "1.0.0",
    Url = "/", // Relative URL - actual host URL is determined by Aspire
    Capabilities = new() { Streaming = true },
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Skills =
    [
        new AgentSkill { Id = "credit-check", Name = "Credit Check", Description = "Check customer credit status, limits, and available credit" },
        new AgentSkill { Id = "pricing", Name = "Pricing Calculator", Description = "Calculate tier-based pricing with customer-specific discounts" },
        new AgentSkill { Id = "quotes", Name = "Quote Management", Description = "Create and track customer quotes with automatic tier pricing" },
        new AgentSkill { Id = "payments", Name = "Payment Processing", Description = "Process payments via credit, invoice, prepaid, or wire transfer" },
        new AgentSkill { Id = "financial-summary", Name = "Financial Reporting", Description = "Provide comprehensive customer financial summaries" }
    ]
};

app.MapA2A(financeAgent, "/", financeAgentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/"));

app.Run();
