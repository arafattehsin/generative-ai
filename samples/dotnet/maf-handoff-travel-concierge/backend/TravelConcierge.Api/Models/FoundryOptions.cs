namespace TravelConcierge.Api.Models;

public sealed class FoundryOptions
{
    public const string EndpointEnvironmentVariable = "MICROSOFT_FOUNDRY_PROJECT_ENDPOINT";
    public const string DefaultDeploymentName = "gpt-5.4";

    public string ProjectEndpoint { get; init; } = string.Empty;
    public string DeploymentName { get; init; } = DefaultDeploymentName;

    public bool IsConfigured =>
        Uri.TryCreate(ProjectEndpoint, UriKind.Absolute, out _);

    public static FoundryOptions FromConfiguration(IConfiguration configuration)
    {
        string endpoint = configuration[EndpointEnvironmentVariable] ?? string.Empty;

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"{EndpointEnvironmentVariable} must be set to a valid Microsoft Foundry project endpoint.");
        }

        return new FoundryOptions
        {
            ProjectEndpoint = endpoint,
            DeploymentName = DefaultDeploymentName,
        };
    }
}
