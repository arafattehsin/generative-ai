using IncidentCommandCenter.Api.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IncidentCommandCenter.Api.Tests;

public sealed class IncidentRepositoryTests
{
    [Fact]
    public void GetById_ReturnsNull_WhenIncidentDoesNotExist()
    {
        var env = new TestHostEnvironment
        {
            ContentRootPath = ResolveBackendRoot(),
        };

        IIncidentRepository repository = new FileIncidentRepository(env);

        var incident = repository.GetById("INC-UNKNOWN");

        Assert.Null(incident);
    }

    [Fact]
    public void GetAll_ReturnsSeededIncidents()
    {
        var env = new TestHostEnvironment
        {
            ContentRootPath = ResolveBackendRoot(),
        };

        IIncidentRepository repository = new FileIncidentRepository(env);

        var incidents = repository.GetAll();

        Assert.True(incidents.Count >= 3);
        Assert.Contains(incidents, i => i.Id == "INC-SC-1001");
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
