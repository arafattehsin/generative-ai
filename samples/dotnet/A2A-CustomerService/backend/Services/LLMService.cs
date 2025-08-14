using Microsoft.SemanticKernel;

namespace A2ACustomerService.Services;


public class LLMService : ILLMService
{
    private readonly Kernel _kernel;

    public LLMService(string endpoint, string apiKey, string deploymentName)
    {
        _kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
            .Build();
    }


    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            return result.ToString();
        }
        catch (Exception)
        {
            return "I apologize, but I'm unable to process your request at the moment. Please try again later.";
        }
    }

    public async Task<string> AnalyzeTicketAsync(string subject, string description)
    {
        var prompt = $@"
Analyze this customer service ticket and provide a brief professional response:

Subject: {subject}
Description: {description}

Provide a helpful, empathetic response that addresses the customer's concern.
Keep the response concise and professional.
";

        return await GenerateTextAsync(prompt, CancellationToken.None);
    }
}
