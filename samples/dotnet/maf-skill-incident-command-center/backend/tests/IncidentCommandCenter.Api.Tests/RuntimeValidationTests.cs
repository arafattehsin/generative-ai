using IncidentCommandCenter.Api.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IncidentCommandCenter.Api.Tests;

public sealed class RuntimeValidationTests
{
    [Fact]
    public void AgentRuntime_Throws_WhenRequiredEnvironmentVariablesAreMissing()
    {
        string? endpointBefore = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? deploymentBefore = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

        try
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME", null);

            var env = new TestHostEnvironment
            {
                ContentRootPath = ResolveBackendRoot(),
            };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new AgentRuntime(env));
            Assert.Contains("AZURE_OPENAI_ENDPOINT", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", endpointBefore);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME", deploymentBefore);
        }
    }

    private static string ResolveBackendRoot()
    {
        string current = AppContext.BaseDirectory;
        DirectoryInfo? dir = new DirectoryInfo(current);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "IncidentCommandCenter.Api.csproj")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not resolve backend project root.");
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "IncidentCommandCenter.Api.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
