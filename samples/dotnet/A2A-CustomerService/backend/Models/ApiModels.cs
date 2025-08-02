namespace A2ACustomerService.Models;

public class AgentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public AgentStatus Status { get; set; } = AgentStatus.Idle;
    public string? CurrentTicket { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
    public string IconName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public enum AgentStatus
{
    Idle,
    Processing,
    Completed,
    Error
}

public class SubmitTicketRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
}

public class ProcessingUpdate
{
    public string TicketId { get; set; } = string.Empty;
    public string UpdateType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
