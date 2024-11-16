using Azure;
using Azure.AI.OpenAI;
using CustomCopilot.Plugins.FlightTracker;
using CustomCopilot.Plugins.PlaceSuggestions;
using CustomCopilot.Plugins.Weather;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.Core;
using static System.Environment;

namespace CustomCopilot
{
    internal class Program
    {

        static async Task Main(string[] args)

        {
            // Create a kernel with the Azure OpenAI chat completion service
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion("gpt-4",
                GetEnvironmentVariable("AOI_ENDPOINT_SWDN")!,
                GetEnvironmentVariable("AOI_KEY_SWDN")!);

            // Load the plugins
            #pragma warning disable SKEXP0050
            builder.Plugins.AddFromType<TimePlugin>();
            builder.Plugins.AddFromObject(new FlightTracker(GetEnvironmentVariable("AVIATIONSTACK_KEY")!), nameof(FlightTracker));
            builder.Plugins.AddFromObject(new Weather(GetEnvironmentVariable("WEATHERAPI_KEY")!), nameof(Weather));
            builder.Plugins.AddFromObject(new PlaceSuggestions(GetEnvironmentVariable("AZUREMAPS_SUBSCRIPTION_KEY")!), nameof(PlaceSuggestions));

            // Build the kernel
            var kernel = builder.Build();

            // Create chat history
            ChatHistory history = [];
            history.AddSystemMessage(@"You're a virtual assistant responsible for only flight tracking, weather updates and finding out
            the right places within Australia after inquiring about the proximity or city. You should not talk anything outside of your scope.
            Your response should be very concise and to the point. For each correct answer, you will get some $10 from me as a reward.
            Be nice with people.");

            // Get chat completion service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Start the conversation
            while (true)
            {
                // Get user input
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("User > ");
                history.AddUserMessage(Console.ReadLine()!);

                #pragma warning disable SKEXP0001
                // Enable auto function calling
                PromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };


                // Get the response from the AI
                var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
                               history,
                               executionSettings: openAIPromptExecutionSettings,
                               kernel: kernel);


                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nAssistant > ");

                string combinedResponse = string.Empty;
                await foreach (var message in response)
                {
                    //Write the response to the console
                    Console.Write(message);
                    combinedResponse += message;
                }

                Console.WriteLine();

                // Add the message from the agent to the chat history
                history.AddAssistantMessage(combinedResponse);
            }
        }
    }
}