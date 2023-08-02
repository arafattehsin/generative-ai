using Azure;
using Azure.AI.OpenAI;
using static System.Environment;

public class AOIHelper
{
    private string endpoint;
    private string key;
    private OpenAIClient client;
    private ChatCompletionsOptions chatCompletionsOptions;

    public AOIHelper()
    {
        this.endpoint = GetEnvironmentVariable("OPEN_AI_ENDPOINT");
        this.key = GetEnvironmentVariable("OPEN_AI_KEY");
        this.client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
        this.chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatMessage(ChatRole.System, "You are a virtual flight assistant who is responsible for tracking flights. You need either a flight number or source (to) and destination (from) to track any flight. If user does not provide either of this information then you should ask for it. Anything outside of the flights are not a part of your scope and you should just answer with an Unknown intent."),
                new ChatMessage(ChatRole.User, "Track my flight from Sydney?"),
                new ChatMessage(ChatRole.Assistant,"Sure! Please provide your destination?"),
                new ChatMessage(ChatRole.User, "I want to track flight JF-123?"),
                new ChatMessage(ChatRole.Assistant, "Your flight JF-123 is now departed from Sydney and will be arriving soon in Melbourne at 7 PM"),
            },
            MaxTokens = 100
        };

    }

    public string GetResponse(string chatMessage)
    {
        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, chatMessage));
        Response<ChatCompletions> response = client.GetChatCompletions(
            deploymentOrModelName: "gpt-4",
            chatCompletionsOptions);

        return response.Value.Choices[0].Message.Content;
    }
}

