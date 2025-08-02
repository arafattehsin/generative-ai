namespace A2ACustomerService.Models;

public class CustomerTicket
{
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public string Category { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public List<string> AssignedAgents { get; set; } = new();
    public List<AgentResponse> Responses { get; set; } = new();
    public string? FinalResponse { get; set; }
}

public enum TicketStatus
{
    New,
    Routing,
    Processing,
    Completed,
    Failed
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Urgent
}
