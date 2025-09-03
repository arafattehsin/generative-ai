namespace A2ACustomerService.Models;

public class AgentResponse
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ResponseStatus Status { get; set; } = ResponseStatus.Pending;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public A2A.Message ToMessage(string? contextId = null)
    {
        // Use provided contextId (e.g., ticket/workflow/correlation ID) if available, else fallback to AgentId
        var metadata = new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["Timestamp"] = System.Text.Json.JsonSerializer.SerializeToElement(Timestamp),
            ["AgentType"] = System.Text.Json.JsonSerializer.SerializeToElement(AgentType),
            ["Status"] = System.Text.Json.JsonSerializer.SerializeToElement(Status.ToString())
        };
        var message = new A2A.Message
        {
            Role = A2A.MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = contextId ?? AgentId,
            Parts = new List<A2A.Part> { new A2A.TextPart { Text = Response } },
            Metadata = metadata
        };
        return message;
    }
}

public enum ResponseStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
