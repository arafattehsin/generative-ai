using A2A;
using A2ACustomerService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace A2ACustomerService.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection AddA2AServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the configuration service as singleton to maintain state
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Register all services (both real and mock)
        // We'll use a factory pattern to decide at runtime

        // Azure OpenAI configuration
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AOI_ENDPOINT_SWDN")
                                  ?? configuration.GetValue<string>("AzureOpenAI:Endpoint");
        var azureOpenAIKey = Environment.GetEnvironmentVariable("AOI_KEY_SWDN")
                             ?? configuration.GetValue<string>("AzureOpenAI:ApiKey");
        var deploymentName = configuration.GetValue<string>("AzureOpenAI:DeploymentName", "gpt-4o");

        // Register LLM service for real implementation as Singleton to work with Singleton agents
        if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
        {
            services.AddSingleton<ILLMService>(provider =>
                new LLMService(azureOpenAIEndpoint, azureOpenAIKey, deploymentName));
        }

        // Register ticket service as Singleton to maintain state
        services.AddSingleton<MockTicketService>();
        services.AddSingleton<A2ATicketService>();

        // Register A2A Customer Service Agent
        services.AddSingleton<CustomerServiceA2AAgent>();

        // Register individual A2A agents for A2ATicketService
        services.AddSingleton<FrontDeskAgent>();
        services.AddSingleton<BillingAgent>();
        services.AddSingleton<TechnicalAgent>();
        services.AddSingleton<OrchestratorAgent>();

        // Register A2A Task Manager for Customer Service Agent
        services.AddSingleton<ITaskManager>(provider =>
        {
            var taskManager = new TaskManager();
            var agent = provider.GetRequiredService<CustomerServiceA2AAgent>();
            agent.Attach(taskManager);
            return taskManager;
        });

        // Register ITicketService based on configuration
        services.AddScoped<ITicketService>(provider =>
        {
            var configService = provider.GetRequiredService<IConfigurationService>();
            if (configService.UseRealImplementation)
            {
                return provider.GetRequiredService<A2ATicketService>();
            }
            return provider.GetRequiredService<MockTicketService>();
        });

        return services;
    }
}
