using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public sealed class FileAgentSkillsProvider : ISkillCatalogService
{
    private readonly string _skillsRoot;
    private readonly Lazy<IReadOnlyList<SkillSummary>> _skillsCache;

    public FileAgentSkillsProvider(IHostEnvironment environment)
    {
        _skillsRoot = Path.Combine(environment.ContentRootPath, "skills");
        _skillsCache = new Lazy<IReadOnlyList<SkillSummary>>(LoadSkills);
    }

    public IReadOnlyList<SkillSummary> GetSkills() => _skillsCache.Value;

    public IReadOnlyList<string> GetResources(string skillName)
    {
        string folder = Path.Combine(_skillsRoot, skillName);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith("SKILL.md", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(folder, file).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string ReadResource(string skillName, string resourcePath)
    {
        string filePath = Path.Combine(_skillsRoot, skillName, resourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Resource '{resourcePath}' was not found for skill '{skillName}'.", filePath);
        }

        return File.ReadAllText(filePath);
    }

    private IReadOnlyList<SkillSummary> LoadSkills()
    {
        if (!Directory.Exists(_skillsRoot))
        {
            return [];
        }

        List<SkillSummary> skills = [];
        foreach (string skillFolder in Directory.GetDirectories(_skillsRoot))
        {
            string skillName = Path.GetFileName(skillFolder);
            string skillDefinitionPath = Path.Combine(skillFolder, "SKILL.md");
            if (!File.Exists(skillDefinitionPath))
            {
                continue;
            }

            string skillText = File.ReadAllText(skillDefinitionPath);
            string parsedName = ParseFrontMatterValue(skillText, "name") ?? skillName;
            string description = ParseFrontMatterValue(skillText, "description")
                ?? "No description found in SKILL.md";

            IReadOnlyList<string> resources = GetResources(skillName);
            skills.Add(new SkillSummary(parsedName, description, resources));
        }

        return skills
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ParseFrontMatterValue(string markdown, string key)
    {
        string[] lines = markdown.Replace("\r", string.Empty).Split('\n');
        if (lines.Length == 0 || lines[0] != "---")
        {
            return null;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line == "---")
            {
                break;
            }

            int colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            string currentKey = line[..colonIndex].Trim();
            if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return line[(colonIndex + 1)..].Trim().Trim('"');
        }

        return null;
    }
}
