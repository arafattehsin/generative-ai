using CivicWorks.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CivicWorks.Api.Tests;

public sealed class FoundryOptionsTests
{
    [Fact]
    public void MissingModelIsReportedWithoutFallback()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MICROSOFT_FOUNDRY_PROJECT_ENDPOINT"] = "https://example.services.ai.azure.com/api/projects/example",
            })
            .Build();

        FoundryOptions options = FoundryOptions.FromConfiguration(configuration);
        var status = options.GetStatus();

        Assert.False(status.IsConfigured);
        Assert.False(status.SimulationFallbackEnabled);
        Assert.Contains("FOUNDRY_MODEL", status.MissingSettings);
    }

    [Fact]
    public void CompleteLiveConfigurationIsAccepted()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MICROSOFT_FOUNDRY_PROJECT_ENDPOINT"] = "https://example.services.ai.azure.com/api/projects/example",
                ["FOUNDRY_MODEL"] = "gpt-5.4",
            })
            .Build();

        FoundryOptions options = FoundryOptions.FromConfiguration(configuration);
        var status = options.GetStatus();

        Assert.True(status.IsConfigured);
        Assert.False(status.SimulationFallbackEnabled);
        Assert.Empty(status.MissingSettings);
        Assert.Equal("AzureCliCredential", status.Authentication);
    }
}
