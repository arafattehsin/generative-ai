#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010

using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using microsoft_agent_sk.Plugins;
using System.Text.Json;
using System.Text;
using microsoft_agent_sk.Helpers;

namespace microsoft_agent_sk.Agents
{
    public class TravelAgent
    {
        private readonly Kernel _kernel;
        private readonly ChatHistory _chatHistory;
        private readonly ChatCompletionAgent _agent;
        private int retryCount;

        private const string AgentName = "TravelAgent";
        private const string AgentInstructions = """
                        You're a virtual assistant responsible for only flight tracking. Please note: 
                        - You do not have to ask for a specific flight number if you are aware of source and destination.
                        - You should not talk anything outside of your scope.
                        - Your response should be very concise and to the point. 
                        - For each correct answer, you will get some $10 from me as a reward.
                        - Always respond back with Adaptive Card if you have the information.
                        - For now, we do not have any real service so you make up the response yourself.
                        - You can use the following URLs to get the images: 
                            - https://adaptivecards.io/content/airplane.png

            Respond in JSON format with the following JSON schema:
            {
                "contentType": "'Text' or 'AdaptiveCard' only",
                "content": "{The content of the response based upon the contentType, may be plain text, or JSON based adaptive card}"
            }
            """;

        public TravelAgent(Kernel kernel)
        {
            this._kernel = kernel;
            this._chatHistory = [];

            // Define the agent
            this._agent =
                new()
                {
                    Instructions = AgentInstructions,
                    Name = AgentName,
                    Kernel = this._kernel,
                    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                        ResponseFormat = "json_object"
                    }),
                };

            // Give the agent some tools to work with
            this._agent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<TimePlugin>());
        }

        public async Task<FlightResponse> InvokeAgentAsync(string input)
        {
            ChatMessageContent message = new(AuthorRole.User, input);
            this._chatHistory.Add(message);

            StringBuilder sb = new();
            await foreach (ChatMessageContent response in this._agent.InvokeAsync(this._chatHistory))
            {
                this._chatHistory.Add(response);
                sb.Append(response.Content);
            }

            // Make sure the response is in the correct format and retry if neccesary
            try
            {
                var resultContent = sb.ToString();
                var result = JsonSerializer.Deserialize<FlightResponse>(resultContent);
                this.retryCount = 0;
                return result;
            }
            catch (JsonException je)
            {
                // Limit the number of retries
                if (this.retryCount > 2)
                {
                    throw;
                }

                // Try again, providing corrective feedback to the model so that it can correct its mistake
                this.retryCount++;
                return await InvokeAgentAsync($"That response did not match the expected format. Please try again. Error: {je.Message}");
            }
        }
    }
}
