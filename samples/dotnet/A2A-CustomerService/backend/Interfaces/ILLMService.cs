using A2A;

namespace A2ACustomerService.Services;

public interface ILLMService
{
    Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken);
}
