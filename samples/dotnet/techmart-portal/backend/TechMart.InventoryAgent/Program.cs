// Copyright (c) Microsoft. All rights reserved.

// TechMart Enterprise Operations Portal - Inventory Agent
// Remote A2A agent for inventory and warehouse operations

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

// Simulated inventory data
Dictionary<string, (int Stock, int Reserved, int Reorder, string Warehouse, string Supplier)> inventory = new()
{
    ["Gaming Laptop ROG Strix"] = (150, 25, 50, "Seattle-WH1", "ASUS Distribution"),
    ["4K Monitor 32-inch"] = (500, 120, 100, "Seattle-WH1", "LG Electronics"),
    ["Mechanical Keyboard Pro"] = (1200, 300, 200, "Portland-WH2", "Logitech"),
    ["Enterprise SSD 2TB"] = (80, 15, 30, "Seattle-WH1", "Samsung Storage"),
    ["Server Rack 42U"] = (12, 3, 5, "Portland-WH2", "APC"),
    ["USB-C Hub Premium"] = (800, 50, 150, "Seattle-WH1", "Anker"),
    ["Graphics Card RTX 4080"] = (45, 12, 20, "Seattle-WH1", "NVIDIA Partners"),
    ["Wireless Headset Pro"] = (350, 80, 100, "Portland-WH2", "Sony Audio"),
};

Dictionary<string, (string Status, DateTime LastUpdate)> warehouses = new()
{
    ["Seattle-WH1"] = ("Operational", DateTime.Now.AddHours(-2)),
    ["Portland-WH2"] = ("Operational", DateTime.Now.AddHours(-1)),
    ["Denver-WH3"] = ("Maintenance", DateTime.Now.AddDays(-1)),
};

// Inventory tool functions
[Description("Get detailed inventory status for a specific product")]
string GetInventoryDetails([Description("The product name to look up")] string productName)
{
    var match = inventory.FirstOrDefault(p =>
        p.Key.Contains(productName, StringComparison.OrdinalIgnoreCase));

    if (match.Key != null)
    {
        var item = match.Value;
        int available = item.Stock - item.Reserved;
        bool needsReorder = available < item.Reorder;

        return $"""
            Product: {match.Key}
            - Total Stock: {item.Stock} units
            - Reserved: {item.Reserved} units
            - Available: {available} units
            - Reorder Point: {item.Reorder} units
            - Warehouse: {item.Warehouse}
            - Supplier: {item.Supplier}
            - Status: {(needsReorder ? "REORDER RECOMMENDED" : "OK")}
            """;
    }
    return $"Product '{productName}' not found in inventory.";
}

[Description("Get all products that need reordering")]
string GetReorderList()
{
    var needsReorder = inventory.Where(p =>
        (p.Value.Stock - p.Value.Reserved) < p.Value.Reorder).ToList();

    if (needsReorder.Count == 0)
        return "No products currently need reordering.";

    return "Products Requiring Reorder:\n" + string.Join("\n", needsReorder.Select(p =>
    {
        int available = p.Value.Stock - p.Value.Reserved;
        int shortage = p.Value.Reorder - available;
        return $"  - {p.Key}: {available} available (need {shortage} more) - Contact: {p.Value.Supplier}";
    }));
}

[Description("Reserve inventory for an order")]
string ReserveInventory([Description("The product name")] string productName, [Description("Quantity to reserve")] int quantity)
{
    var match = inventory.FirstOrDefault(p =>
        p.Key.Contains(productName, StringComparison.OrdinalIgnoreCase));

    if (match.Key == null)
        return $"Product '{productName}' not found.";

    var item = match.Value;
    int available = item.Stock - item.Reserved;

    if (quantity > available)
        return $"Cannot reserve {quantity} units. Only {available} available.";

    inventory[match.Key] = (item.Stock, item.Reserved + quantity, item.Reorder, item.Warehouse, item.Supplier);
    return $"Reserved {quantity} units of {match.Key}. Remaining available: {available - quantity}";
}

[Description("Get warehouse status information")]
string GetWarehouseStatus([Description("Warehouse ID (e.g., Seattle-WH1)")] string warehouseId)
{
    if (warehouses.TryGetValue(warehouseId, out var wh))
        return $"Warehouse {warehouseId}: Status={wh.Status}, Last Updated={wh.LastUpdate:g}";
    return $"Warehouse '{warehouseId}' not found. Available: {string.Join(", ", warehouses.Keys)}";
}

[Description("Transfer stock between warehouses")]
string TransferStock([Description("Product name")] string productName, [Description("Number of units")] int quantity, [Description("Source warehouse")] string fromWarehouse, [Description("Destination warehouse")] string toWarehouse)
{
    return $"Transfer initiated: {quantity} units of {productName} from {fromWarehouse} to {toWarehouse}. ETA: 2-3 business days.";
}

// Create the Inventory Agent
AIAgent inventoryAgent = chatClient.CreateAIAgent(
    name: "InventoryAgent",
    instructions: """
        You are the TechMart Inventory Agent, responsible for all inventory and warehouse operations.
        
        Your capabilities include:
        1. Check product inventory levels and availability
        2. Reserve inventory for orders
        3. Identify products that need reordering
        4. Monitor warehouse status
        5. Coordinate stock transfers between warehouses
        
        Always provide accurate inventory information and warn about low stock situations.
        """,
    tools: [
        AIFunctionFactory.Create(GetInventoryDetails),
        AIFunctionFactory.Create(GetReorderList),
        AIFunctionFactory.Create(ReserveInventory),
        AIFunctionFactory.Create(GetWarehouseStatus),
        AIFunctionFactory.Create(TransferStock)
    ]);

// Expose as A2A agent
AgentCard inventoryAgentCard = new()
{
    Name = "TechMart Inventory Agent",
    Description = "Handles inventory management, stock levels, warehouse operations, and reordering for TechMart retail operations",
    Version = "1.0.0",
    Url = "/", // Relative URL - actual host URL is determined by Aspire
    Capabilities = new() { Streaming = true },
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Skills =
    [
        new AgentSkill { Id = "inventory-check", Name = "Inventory Check", Description = "Check product stock levels and availability" },
        new AgentSkill { Id = "reservation", Name = "Stock Reservation", Description = "Reserve inventory for orders" },
        new AgentSkill { Id = "reorder", Name = "Reorder Management", Description = "Identify and manage product reordering" },
        new AgentSkill { Id = "warehouse", Name = "Warehouse Operations", Description = "Monitor warehouse status and transfers" }
    ]
};

app.MapA2A(inventoryAgent, "/", inventoryAgentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/"));

app.Run();
