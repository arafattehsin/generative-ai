using System.Text.Json;
using Azure; // AzureKeyCredential
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.ChatClient;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// CORS for local Vite
var frontendOrigin = builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
    .WithOrigins(frontendOrigin, "http://localhost:5000", "http://localhost:5001", "http://localhost:5002")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Local seed function for invoices
Dictionary<string, InvoiceDto> SeedInvoices()
{
    return new[]
    {
        new InvoiceDto("INV-1001", "Acme Supplies", 850.00m, "USD", "Paid"),
        new InvoiceDto("INV-1002", "Northwind Services", 2450.00m, "USD", "Pending"),
        new InvoiceDto("INV-1003", "TechCorp Solutions", 1200.00m, "USD", "Paid"),
        new InvoiceDto("INV-1004", "Office Depot", 320.75m, "USD", "Pending"),
        new InvoiceDto("INV-1005", "Global Industries", 5400.00m, "USD", "Pending")
    }.ToDictionary(i => i.InvoiceId, i => i);
}

// Simple in-memory stores
var sessions = new Dictionary<string, AgentThread>();
var invoices = SeedInvoices();
// Track latest approval requests per session to map requestId -> original request
var pendingApprovals = new Dictionary<string, Dictionary<string, FunctionApprovalRequestContent>>();
// Track invoices explicitly approved for high-value payments
var approvedInvoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

// Configure Azure OpenAI ChatClient and Agent
var endpoint = Environment.GetEnvironmentVariable("AOI_ENDPOINT_SWDN")
    ?? throw new InvalidOperationException("AOI_ENDPOINT_SWDN is not set.");
var apiKey = Environment.GetEnvironmentVariable("AOI_KEY_SWDN")
    ?? throw new InvalidOperationException("AOI_KEY_SWDN is not set.");
var deployment = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-4.1";

var openAiClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
var chatClient = openAiClient.GetChatClient(deployment);

// Define functions
string GetInvoiceStatus(string invoiceId)
{
    if (!invoices.TryGetValue(invoiceId, out var inv)) throw new ArgumentException("Invoice not found");
    return JsonSerializer.Serialize(new { invoiceId = inv.InvoiceId, status = inv.Status, amount = inv.Amount, vendorName = inv.VendorName });
}

string ReleasePayment(string invoiceId)
{
    if (!invoices.TryGetValue(invoiceId, out var inv)) throw new ArgumentException("Invoice not found");
    if (inv.Status == "Paid") return JsonSerializer.Serialize(new { invoiceId = inv.InvoiceId, status = inv.Status });
    // Enforce approval for high-value payments
    if (inv.Amount > 1000m && !approvedInvoices.Contains(invoiceId))
    {
        return JsonSerializer.Serialize(new { invoiceId = inv.InvoiceId, status = inv.Status });
    }
    // Replace immutable record instance with updated one
    var updated = inv with { Status = "Paid" };
    invoices[invoiceId] = updated;
    return JsonSerializer.Serialize(new { invoiceId = updated.InvoiceId, status = updated.Status });
}

// Define tools and wrap release with approval requirement
var getStatusTool = AIFunctionFactory.Create(GetInvoiceStatus);
var releasePaymentTool = AIFunctionFactory.Create(ReleasePayment);
var releasePaymentWithApproval = new ApprovalRequiredAIFunction(releasePaymentTool);

var agent = chatClient.CreateAIAgent(
    instructions: "You are a spend-control assistant. Use tools for invoice status and payments.",
    tools: [getStatusTool, releasePaymentWithApproval]);

var app = builder.Build();

app.UseCors();

app.MapPost("/api/session", () =>
{
    var id = $"session-{Guid.NewGuid():N}";
    sessions[id] = agent.GetNewThread();
    pendingApprovals[id] = new Dictionary<string, FunctionApprovalRequestContent>();
    Console.WriteLine($"[SESSION] Created {id}");
    return Results.Ok(new { sessionId = id });
});

app.MapGet("/api/invoices", () => Results.Ok(invoices.Values.OrderBy(i => i.InvoiceId)));

app.MapPost("/api/actions/get-status", async (ActionRequest req) =>
{
    if (!sessions.TryGetValue(req.SessionId, out var thread)) return Results.BadRequest(new { error = "Invalid session" });
    // Prefer agent, but always fall back to store on failure
    try
    {
        var sysMsg = new ChatMessage(ChatRole.User, [new TextContent($"Get status for invoice {req.InvoiceId}")]);
        var response = await agent.RunAsync([sysMsg], thread);
        var json = response.Text;
        try
        {
            var parsed = JsonSerializer.Deserialize<InvoiceStatusResult>(json ?? "{}");
            if (parsed is not null)
            {
                return Results.Ok(new { result = parsed, userInputRequests = Array.Empty<object>() });
            }
        }
        catch
        {
            // Fall through to store
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STATUS] Agent call failed: {ex.Message}");
    }

    // Fallback: build from store
    if (!invoices.TryGetValue(req.InvoiceId, out var inv)) return Results.BadRequest(new { error = "Invoice not found" });
    var result = new InvoiceStatusResult(inv.InvoiceId, inv.Status, inv.Amount, inv.VendorName);
    return Results.Ok(new { result, userInputRequests = Array.Empty<object>() });
});

app.MapPost("/api/actions/release-payment", async (ActionRequest req) =>
{
    if (!sessions.TryGetValue(req.SessionId, out var thread)) return Results.BadRequest(new { error = "Invalid session" });
    if (!invoices.TryGetValue(req.InvoiceId, out var inv)) return Results.BadRequest(new { error = "Invoice not found" });

    Console.WriteLine($"[PAYMENT] Processing payment for {req.InvoiceId}: ${inv.Amount} (threshold: $1000)");

    // Approval threshold: auto-execute when amount <= 1000, otherwise ask the agent (which will request approval)
    if (inv.Amount <= 1000m)
    {
        Console.WriteLine($"[PAYMENT] Auto-approving payment <= $1000 for {req.InvoiceId}");
        // Directly execute without agent approval
        var json = ReleasePayment(req.InvoiceId);
        var result = JsonSerializer.Deserialize<PaymentResult>(json ?? "{}") ?? new PaymentResult(req.InvoiceId, invoices[req.InvoiceId].Status);
        return Results.Ok(new { result, userInputRequests = Array.Empty<object>() });
    }

    // Request via agent, which will produce FunctionApprovalRequestContent
    var userMsg = new ChatMessage(ChatRole.User, [new TextContent($"Release payment for invoice {req.InvoiceId}")]);
    Console.WriteLine($"[PAYMENT] Asking agent to release payment for {req.InvoiceId} (${inv.Amount})");
    var response = await agent.RunAsync([userMsg], thread);
    Console.WriteLine($"[PAYMENT] Agent response UserInputRequests count: {response.UserInputRequests.Count()}");
    var approvals = response.UserInputRequests.OfType<FunctionApprovalRequestContent>().ToArray();
    Console.WriteLine($"[PAYMENT] Found {approvals.Length} approval requests");

    if (approvals.Length > 0)
    {
        var map = pendingApprovals[req.SessionId];
        foreach (var a in approvals)
        {
            map[a.Id] = a;
        }

        // Enrich function call arguments with invoice details to stabilize frontend rendering
        var shaped = approvals.Select(a =>
            {
                var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (a.FunctionCall.Arguments != null)
                {
                    foreach (var kv in a.FunctionCall.Arguments)
                    {
                        args[kv.Key] = kv.Value;
                    }
                }

                string? invId = null;
                if (args.TryGetValue("invoiceId", out var invoiceIdObj))
                {
                    invId = Convert.ToString(invoiceIdObj);
                }
                invId ??= req.InvoiceId;

                if (!string.IsNullOrWhiteSpace(invId) && invoices.TryGetValue(invId, out var inv2))
                {
                    args["invoiceId"] = inv2.InvoiceId;
                    args["amount"] = inv2.Amount;
                    args["currency"] = inv2.Currency;
                    args["vendorName"] = inv2.VendorName;
                }

                return new ApprovalRequestDto(
                    a.Id,
                    "function-approval-request",
                    // Show a friendly function name in the UI
                    new FunctionCallDto("releasePayment", args)
                );
            }).ToArray();

        return Results.Ok(new { result = (object?)null, userInputRequests = shaped });
    }

    // If agent executed regardless, return its result
    {
        var json = response.Text;
        try
        {
            var result = JsonSerializer.Deserialize<PaymentResult>(json ?? "{}");
            return Results.Ok(new { result, userInputRequests = Array.Empty<object>() });
        }
        catch
        {
            return Results.Ok(new { result = new PaymentResult(inv.InvoiceId, invoices[inv.InvoiceId].Status), userInputRequests = Array.Empty<object>() });
        }
    }
});

app.MapPost("/api/approvals", async (ApprovalSubmitRequest req) =>
{
    if (!sessions.TryGetValue(req.SessionId, out var thread)) return Results.BadRequest(new { error = "Invalid session" });

    // If any approval is denied, short-circuit and do NOT execute the function.
    if (req.Approvals.Any(a => !a.Approved))
    {
        string? affectedInvoiceId = null;
        if (pendingApprovals.TryGetValue(req.SessionId, out var mapToClear))
        {
            foreach (var dec in req.Approvals)
            {
                if (mapToClear.TryGetValue(dec.RequestId, out var original))
                {
                    if (original.FunctionCall.Arguments.TryGetValue("invoiceId", out var idObj))
                    {
                        var invId = Convert.ToString(idObj);
                        if (!string.IsNullOrWhiteSpace(invId))
                        {
                            affectedInvoiceId ??= invId;
                            // Set status to Rejected on denial
                            if (invoices.TryGetValue(invId, out var inv))
                            {
                                invoices[invId] = inv with { Status = "Rejected" };
                            }
                            // Ensure any pre-staged approval flag is cleared
                            approvedInvoices.Remove(invId);
                        }
                    }
                }
                mapToClear.Remove(dec.RequestId);
            }
        }
        Console.WriteLine("[APPROVALS] One or more approvals denied; status set to Rejected.");
        if (!string.IsNullOrWhiteSpace(affectedInvoiceId) && invoices.TryGetValue(affectedInvoiceId, out var updated))
        {
            return Results.Ok(new { result = new PaymentResult(updated.InvoiceId, updated.Status), userInputRequests = Array.Empty<object>() });
        }
        return Results.Ok(new { result = (object?)null, userInputRequests = Array.Empty<object>() });
    }

    // Submit approvals by directly emitting FunctionApprovalResponseContent to the thread
    var contents = new List<AIContent>();
    if (pendingApprovals.TryGetValue(req.SessionId, out var map))
    {
        foreach (var dec in req.Approvals)
        {
            if (map.TryGetValue(dec.RequestId, out var original))
            {
                contents.Add(original.CreateResponse(dec.Approved));
                if (dec.Approved)
                {
                    if (original.FunctionCall.Arguments.TryGetValue("invoiceId", out var idObj))
                    {
                        var invId = Convert.ToString(idObj);
                        if (!string.IsNullOrWhiteSpace(invId))
                        {
                            approvedInvoices.Add(invId);
                        }
                    }
                }
                else
                {
                    // Cleanup entry on denial
                    map.Remove(dec.RequestId);
                }
            }
        }
    }

    if (contents.Count == 0)
    {
        return Results.BadRequest(new { error = "No matching approval requests" });
    }

    var msg = new ChatMessage(ChatRole.User, contents);
    var response = await agent.RunAsync([msg], thread);
    var json = response.Text;
    try
    {
        var result = JsonSerializer.Deserialize<PaymentResult>(json ?? "{}");
        if (result is not null && string.Equals(result.Status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            // Best-effort cleanup of tokens for this invoice from this session
            if (pendingApprovals.TryGetValue(req.SessionId, out var map3))
            {
                foreach (var kv in map3.ToArray())
                {
                    if (kv.Value.FunctionCall.Arguments.TryGetValue("invoiceId", out var idObj))
                    {
                        var invId = Convert.ToString(idObj);
                        if (!string.IsNullOrWhiteSpace(invId))
                        {
                            approvedInvoices.Remove(invId);
                        }
                    }
                }
            }
        }
        return Results.Ok(new { result, userInputRequests = Array.Empty<object>() });
    }
    catch
    {
        // Fallback to current invoice state if available
        if (pendingApprovals.TryGetValue(req.SessionId, out var map2))
        {
            var firstReqId = req.Approvals.FirstOrDefault()?.RequestId;
            if (!string.IsNullOrEmpty(firstReqId) && map2.TryGetValue(firstReqId, out var original))
            {
                var invIdObj = original.FunctionCall.Arguments.TryGetValue("invoiceId", out var v) ? v : null;
                var invId = Convert.ToString(invIdObj);
                if (!string.IsNullOrWhiteSpace(invId) && invoices.TryGetValue(invId, out var inv))
                {
                    return Results.Ok(new { result = new PaymentResult(inv.InvoiceId, inv.Status), userInputRequests = Array.Empty<object>() });
                }
            }
        }
        return Results.Ok(new { result = (object?)null, userInputRequests = Array.Empty<object>() });
    }
});

app.Run();

// Models and seed data
record ActionRequest(string SessionId, string InvoiceId);
record ApprovalDecisionDto(string RequestId, bool Approved);
record ApprovalSubmitRequest(string SessionId, IReadOnlyList<ApprovalDecisionDto> Approvals);
record FunctionCallDto(string Name, IDictionary<string, object?> Arguments);
record ApprovalRequestDto(string Id, string Type, FunctionCallDto FunctionCall);

record InvoiceDto(string InvoiceId, string VendorName, decimal Amount, string Currency, string Status);

record InvoiceStatusResult(string InvoiceId, string Status, decimal Amount, string VendorName);
record PaymentResult(string InvoiceId, string Status);
