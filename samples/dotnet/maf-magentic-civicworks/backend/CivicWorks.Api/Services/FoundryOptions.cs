using CivicWorks.Api.Models;

namespace CivicWorks.Api.Services;

public sealed record FoundryOptions(string? ProjectEndpoint, string? ModelDeployment)
{
    public static FoundryOptions FromConfiguration(IConfiguration configuration) => new(
        configuration["MICROSOFT_FOUNDRY_PROJECT_ENDPOINT"],
        configuration["FOUNDRY_MODEL"]);

    public FoundryConfigurationStatus GetStatus()
    {
        List<string> missing = [];

        if (!Uri.TryCreate(ProjectEndpoint, UriKind.Absolute, out _))
        {
            missing.Add("MICROSOFT_FOUNDRY_PROJECT_ENDPOINT");
        }

        if (string.IsNullOrWhiteSpace(ModelDeployment))
        {
            missing.Add("FOUNDRY_MODEL");
        }

        return new FoundryConfigurationStatus(
            missing.Count == 0,
            SimulationFallbackEnabled: false,
            Authentication: "AzureCliCredential",
            ModelDeployment,
            missing);
    }

    public void Validate()
    {
        FoundryConfigurationStatus status = GetStatus();
        if (!status.IsConfigured)
        {
            throw new InvalidOperationException(
                $"Live Microsoft Foundry configuration is incomplete. Set: {string.Join(", ", status.MissingSettings)}. " +
                "CivicWorks has no simulated or scripted fallback.");
        }
    }
}
