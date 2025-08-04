using A2A;

namespace A2ACustomerService.Services;

/// <summary>
/// A2A-enabled Customer Service Agent that handles customer support tickets
/// </summary>
public class CustomerServiceA2AAgent
{
    private readonly ILLMService _llmService;
    private readonly ILogger<CustomerServiceA2AAgent> _logger;

    public CustomerServiceA2AAgent(ILLMService llmService, ILogger<CustomerServiceA2AAgent> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = ProcessMessageAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    public async Task<Message> ProcessMessageAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        return await ProcessMessageInternalAsync(messageSendParams, cancellationToken);
    }

    private async Task<Message> ProcessMessageInternalAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return CreateCancelledMessage(messageSendParams.Message);
        }

        try
        {
            _logger.LogInformation("CustomerServiceA2AAgent: Processing message");

            // Extract text from the message
            var textPart = messageSendParams.Message.Parts.OfType<TextPart>().FirstOrDefault();
            if (textPart == null)
            {
                return CreateErrorMessage(messageSendParams.Message, "No text content found in message");
            }

            // Process the customer service request using the LLM
            var customerServicePrompt = $@"
You are a professional customer service agent. A customer has sent the following message:

{textPart.Text}

Please provide a helpful, empathetic, and professional response that:
1. Acknowledges their concern
2. Provides relevant assistance or information
3. Offers next steps if applicable
4. Maintains a friendly and professional tone

Keep your response concise but comprehensive.
";

            var responseText = await _llmService.GenerateResponseAsync(customerServicePrompt);

            // Create and return a response message
            var message = new Message()
            {
                Role = MessageRole.Agent,
                MessageId = Guid.NewGuid().ToString(),
                ContextId = messageSendParams.Message.ContextId,
                Parts = [new TextPart() {
                    Text = responseText
                }]
            };

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in CustomerServiceA2AAgent");
            return CreateErrorMessage(messageSendParams.Message, "An error occurred while processing your request. Please try again.");
        }
    }

    public Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AgentCard>(cancellationToken);
        }

        var capabilities = new AgentCapabilities()
        {
            Streaming = true,
            PushNotifications = false,
        };

        return Task.FromResult(new AgentCard()
        {
            Name = "Customer Service Agent",
            Description = "AI-powered customer service agent that helps with support tickets, billing inquiries, technical issues, and general customer assistance.",
            Url = agentUrl,
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [],
        });
    }

    private Message CreateErrorMessage(Message originalMessage, string errorText)
    {
        return new Message()
        {
            Role = MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = originalMessage.ContextId,
            Parts = [new TextPart() {
                Text = errorText
            }]
        };
    }

    private Message CreateCancelledMessage(Message originalMessage)
    {
        return new Message()
        {
            Role = MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = originalMessage.ContextId,
            Parts = [new TextPart() {
                Text = "Request was cancelled."
            }]
        };
    }
}
