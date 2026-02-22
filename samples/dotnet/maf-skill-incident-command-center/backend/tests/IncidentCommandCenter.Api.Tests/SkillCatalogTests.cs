using IncidentCommandCenter.Api.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IncidentCommandCenter.Api.Tests;

public sealed class SkillCatalogTests
{
    [Fact]
    public void GetSkills_ReturnsExpectedSkillNames()
    {
        var env = new TestHostEnvironment
        {
            ContentRootPath = ResolveBackendRoot(),
        };

        ISkillCatalogService catalog = new FileAgentSkillsProvider(env);

        var skills = catalog.GetSkills();

        Assert.Contains(skills, s => s.Name == "incident-triage");
        Assert.Contains(skills, s => s.Name == "incident-communications");
    }

    [Fact]
    public void GetResources_ReturnsExpectedReferences()
    {
        var env = new TestHostEnvironment
        {
            ContentRootPath = ResolveBackendRoot(),
        };

        ISkillCatalogService catalog = new FileAgentSkillsProvider(env);
        var resources = catalog.GetResources("incident-triage");

        Assert.Contains("references/SLA_POLICY.md", resources);
        Assert.Contains("assets/triage-report-template.md", resources);
    }

    [Fact]
    public void ReadSkillDefinition_ReturnsSkillMarkdown()
    {
        var env = new TestHostEnvironment
        {
            ContentRootPath = ResolveBackendRoot(),
        };

        ISkillCatalogService catalog = new FileAgentSkillsProvider(env);

        string resolved = catalog.ResolveSkillName("incident-triage");
        string skillMarkdown = catalog.ReadSkillDefinition(resolved);

        Assert.Equal("incident-triage", resolved);
        Assert.Contains("Incident Triage Skill", skillMarkdown);
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
