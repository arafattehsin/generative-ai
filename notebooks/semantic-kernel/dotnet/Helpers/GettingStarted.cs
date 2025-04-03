#pragma warning disable SKEXP0110, SKEXP0001
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System;

public class GettingStarted
{
    string azureOpenAIKey = Environment.GetEnvironmentVariable("AOI_KEY_SWDN");
    string azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AOI_ENDPOINT_SWDN");
    string googleGeminiAPIKey = Environment.GetEnvironmentVariable("GOOGLE_GEMINI_KEY");
    string ollamaEndpoint = "http://localhost:11434";

    public static async Task InvokeAgentAsync(string input, ChatCompletionAgent agent, ChatHistory chat)
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        chat.Add(message);

        await foreach (ChatMessageContent response in agent.InvokeAsync(chat))
        {
            chat.Add(response);
            Console.WriteLine(response);
        }
    }

    public Kernel CreateKernelWithChatCompletion()
    {
        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion("gpt-4", azureOpenAIEndpoint, azureOpenAIKey, "gpt-4-service");

#pragma warning disable SKEXP0070
        builder.AddGoogleAIGeminiChatCompletion(modelId: "gemini-1.5-flash-latest", apiKey: googleGeminiAPIKey, apiVersion: GoogleAIVersion.V1_Beta, serviceId: "gemini-service");

        builder.AddOllamaChatCompletion("llama3.2", new Uri(ollamaEndpoint), "ollama-service");

        return builder.Build();
    }
}