using A2ACustomerService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace A2ACustomerService.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection AddA2AServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Read configuration
        var useRealA2A = configuration.GetValue<bool>("A2A:UseRealImplementation", false);
        var azureOpenAIEndpoint = configuration.GetValue<string>("AzureOpenAI:Endpoint");
        var azureOpenAIKey = configuration.GetValue<string>("AzureOpenAI:ApiKey");
        var deploymentName = configuration.GetValue<string>("AzureOpenAI:DeploymentName", "gpt-4o");

        // Register services based on configuration
        if (useRealA2A && !string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
        {
            // Configure real A2A implementation
            services.AddScoped<ILLMService>(provider =>
                new LLMService(azureOpenAIEndpoint, azureOpenAIKey, deploymentName));
            services.AddScoped<ITicketService, A2ATicketService>();

            // Register A2A agents
            services.AddScoped<FrontDeskAgent>();
            services.AddScoped<BillingAgent>();
            services.AddScoped<TechnicalAgent>();
        }
        else
        {
            // Use mock implementation
            services.AddScoped<ITicketService, MockTicketService>();
        }

        return services;
    }
}
