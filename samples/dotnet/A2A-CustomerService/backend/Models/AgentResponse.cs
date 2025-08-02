namespace A2ACustomerService.Models;

public class AgentResponse
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ResponseStatus Status { get; set; } = ResponseStatus.Pending;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ResponseStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
