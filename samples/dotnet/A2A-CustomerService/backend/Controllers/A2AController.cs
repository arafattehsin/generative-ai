using A2A;
using Microsoft.AspNetCore.Mvc;
using A2ACustomerService.Services;

namespace A2ACustomerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class A2AController : ControllerBase
{
    private readonly ITaskManager _taskManager;
    private readonly CustomerServiceA2AAgent _agent;
    private readonly ILogger<A2AController> _logger;

    public A2AController(ITaskManager taskManager, CustomerServiceA2AAgent agent, ILogger<A2AController> logger)
    {
        _taskManager = taskManager;
        _agent = agent;
        _logger = logger;
    }

    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text))
            {
                return BadRequest("Message text is required");
            }

            // Create a message using A2A protocol
            var message = new Message()
            {
                Role = MessageRole.User,
                MessageId = Guid.NewGuid().ToString(),
                ContextId = request.ContextId ?? Guid.NewGuid().ToString(),
                Parts = [new TextPart() { Text = request.Text }]
            };

            // Create message send parameters
            var messageSendParams = new MessageSendParams
            {
                Message = message
            };

            // Process the message through the A2A agent
            var response = await _agent.ProcessMessageAsync(messageSendParams, CancellationToken.None);

            return Ok(new
            {
                MessageId = response.MessageId,
                ContextId = response.ContextId,
                Response = response.Parts.OfType<TextPart>().FirstOrDefault()?.Text,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing A2A message");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("agent-card")]
    public async Task<IActionResult> GetAgentCard()
    {
        try
        {
            var agentUrl = $"{Request.Scheme}://{Request.Host}/api/a2a";
            var agentCard = await _agent.GetAgentCardAsync(agentUrl, CancellationToken.None);

            return Ok(agentCard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent card");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            AgentType = "customer-service-agent",
            Status = "active",
            Capabilities = new
            {
                Streaming = true,
                PushNotifications = false
            },
            Timestamp = DateTime.UtcNow
        });
    }
}

public class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
    public string? ContextId { get; set; }
}
