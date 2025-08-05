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
    public string? FinalResponse { get; set; }
    public List<AgentResponse> Responses { get; set; } = new();
    public static CustomerTicket FromMessage(A2A.Message message)
    {
        var ticket = new CustomerTicket();
        ticket.Id = message.ContextId ?? Guid.NewGuid().ToString();
        ticket.Timestamp = DateTime.UtcNow;
        foreach (var part in message.Parts)
        {
            if (part is A2A.TextPart textPart)
            {
                // Simple extraction: treat first part as description, others as subject/category if present
                if (string.IsNullOrEmpty(ticket.Description))
                    ticket.Description = textPart.Text;
                else if (string.IsNullOrEmpty(ticket.Subject))
                    ticket.Subject = textPart.Text;
            }
        }
        // Optionally extract more fields from message metadata if available
        return ticket;
    }
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
