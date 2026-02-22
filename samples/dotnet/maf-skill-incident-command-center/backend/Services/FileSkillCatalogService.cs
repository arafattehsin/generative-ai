using IncidentCommandCenter.Api.Models;

namespace IncidentCommandCenter.Api.Services;

public sealed class FileAgentSkillsProvider : ISkillCatalogService
{
    private readonly string _skillsRoot;
    private readonly Lazy<IReadOnlyList<SkillEntry>> _entriesCache;

    public FileAgentSkillsProvider(IHostEnvironment environment)
    {
        _skillsRoot = Path.Combine(environment.ContentRootPath, "skills");
        _entriesCache = new Lazy<IReadOnlyList<SkillEntry>>(LoadSkillEntries);
    }

    public IReadOnlyList<SkillSummary> GetSkills() => _entriesCache.Value
        .Select(entry => new SkillSummary(entry.Name, entry.Description, entry.Resources))
        .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public string ResolveSkillName(string skillName)
    {
        SkillEntry entry = ResolveEntry(skillName);
        return entry.FolderName;
    }

    public string ReadSkillDefinition(string skillName)
    {
        SkillEntry entry = ResolveEntry(skillName);
        return File.ReadAllText(entry.SkillDefinitionPath);
    }

    public IReadOnlyList<string> GetResources(string skillName)
    {
        SkillEntry entry = ResolveEntry(skillName);
        return entry.Resources;
    }

    public string ReadResource(string skillName, string resourcePath)
    {
        SkillEntry entry = ResolveEntry(skillName);
        string filePath = Path.Combine(_skillsRoot, entry.FolderName, resourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Resource '{resourcePath}' was not found for skill '{skillName}'.", filePath);
        }

        return File.ReadAllText(filePath);
    }

    private SkillEntry ResolveEntry(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            throw new ArgumentException("Skill name is required.", nameof(skillName));
        }

        string normalized = skillName.Trim();

        SkillEntry? entry = _entriesCache.Value.FirstOrDefault(candidate =>
            string.Equals(candidate.FolderName, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate.Name, normalized, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            throw new InvalidOperationException($"Skill '{skillName}' was not found.");
        }

        return entry;
    }

    private IReadOnlyList<SkillEntry> LoadSkillEntries()
    {
        if (!Directory.Exists(_skillsRoot))
        {
            return [];
        }

        List<SkillEntry> skills = [];
        foreach (string skillFolder in Directory.GetDirectories(_skillsRoot))
        {
            string folderName = Path.GetFileName(skillFolder);
            string skillDefinitionPath = Path.Combine(skillFolder, "SKILL.md");
            if (!File.Exists(skillDefinitionPath))
            {
                continue;
            }

            string skillText = File.ReadAllText(skillDefinitionPath);
            string parsedName = ParseFrontMatterValue(skillText, "name") ?? folderName;
            string description = ParseFrontMatterValue(skillText, "description")
                ?? "No description found in SKILL.md";

            IReadOnlyList<string> resources = Directory.GetFiles(skillFolder, "*", SearchOption.AllDirectories)
                .Where(file => !file.EndsWith("SKILL.md", StringComparison.OrdinalIgnoreCase))
                .Select(file => Path.GetRelativePath(skillFolder, file).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            skills.Add(new SkillEntry(
                FolderName: folderName,
                Name: parsedName,
                Description: description,
                SkillDefinitionPath: skillDefinitionPath,
                Resources: resources));
        }

        return skills;
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

    private sealed record SkillEntry(
        string FolderName,
        string Name,
        string Description,
        string SkillDefinitionPath,
        IReadOnlyList<string> Resources);
}
