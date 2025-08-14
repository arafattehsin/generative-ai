using A2ACustomerService.Models;
using Microsoft.Extensions.Configuration;

namespace A2ACustomerService.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly string? _azureOpenAIEndpoint;
    private readonly string? _azureOpenAIKey;
    private bool _useRealImplementation;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;

        // Load Azure OpenAI configuration from environment variables or config
        _azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AOI_ENDPOINT_SWDN")
                              ?? _configuration.GetValue<string>("AzureOpenAI:Endpoint");
        _azureOpenAIKey = Environment.GetEnvironmentVariable("AOI_KEY_SWDN")
                         ?? _configuration.GetValue<string>("AzureOpenAI:ApiKey");

        // Initialize implementation preference from config/environment
        _useRealImplementation = _configuration.GetValue<bool>("A2A:UseRealImplementation", false);
        var useRealFromEnv = Environment.GetEnvironmentVariable("A2A_USE_REAL_IMPLEMENTATION");
        if (!string.IsNullOrEmpty(useRealFromEnv))
        {
            _useRealImplementation = bool.Parse(useRealFromEnv);
        }
    }

    public bool UseRealImplementation => _useRealImplementation && HasValidAzureOpenAIConfig;

    public bool HasValidAzureOpenAIConfig =>
        !string.IsNullOrEmpty(_azureOpenAIEndpoint) && !string.IsNullOrEmpty(_azureOpenAIKey);

    public void SetImplementation(bool useReal)
    {
        _useRealImplementation = useReal;
    }

    public ImplementationStatus GetStatus()
    {
        return new ImplementationStatus
        {
            Implementation = UseRealImplementation ? "real" : "mock",
            Timestamp = DateTime.UtcNow.ToString("O")
        };
    }
}
