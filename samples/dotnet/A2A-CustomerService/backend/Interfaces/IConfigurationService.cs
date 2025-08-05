using A2ACustomerService.Models;

namespace A2ACustomerService.Services;

public interface IConfigurationService
{
    bool UseRealImplementation { get; }
    bool HasValidAzureOpenAIConfig { get; }
    void SetImplementation(bool useReal);
    ImplementationStatus GetStatus();
}
