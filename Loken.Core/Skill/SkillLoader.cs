namespace Loken.Core;

public record Skill(string Name, string Description, string Body);

public class SkillLoader(string directory)
{
    private const string SKILL_FILENAME = "SKILL.md";
    private readonly Dictionary<string, Skill> _skills = LoadSkills(directory);

    private static Dictionary<string, Skill> LoadSkills(string directory)
    {
        var loadedSkills = new Dictionary<string, Skill>();

        if (!Directory.Exists(directory))
            return loadedSkills;

        var subDirectories = Directory.GetDirectories(directory);

        foreach (var subDir in subDirectories)
        {
            string skillFilePath = Path.Combine(subDir, SKILL_FILENAME);

            if (!File.Exists(skillFilePath))
                continue;

            try
            {
                string text = File.ReadAllText(skillFilePath);
                var (meta, body) = ParseFrontmatter(text);
                string skillName = meta.GetValueOrDefault("name") ?? Path.GetFileName(subDir);

                if (!meta.TryGetValue("description", out var description))
                    continue;

                loadedSkills[skillName] = new Skill(
                    Name: skillName,
                    Description: description,
                    Body: body.Trim()
                );
            }
            catch
            {
                continue;
            }
        }

        return loadedSkills;
    }

    private static (Dictionary<string, string> Meta, string Body) ParseFrontmatter(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        if (lines.Count == 0 || lines[0].Trim() != "---")
            return (new Dictionary<string, string>(), text);

        var meta = new Dictionary<string, string>();
        int? closingIndex = null;

        for (int i = 1; i < lines.Count; i++)
        {
            var line = lines[i].Trim();

            if (line == "---")
            {
                closingIndex = i;
                break;
            }

            int colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                string key = line[..colonIndex].Trim();
                string value = line[(colonIndex + 1)..].Trim();

                if (!string.IsNullOrEmpty(key))
                    meta[key] = value;
            }
        }

        if (!closingIndex.HasValue)
            return (new Dictionary<string, string>(), text);

        var bodyLines = lines.Skip(closingIndex.Value + 1);
        string body = string.Join("\n", bodyLines);

        return (meta, body);
    }

    public string GetSkills() =>
      string.Join("\n", _skills.Values
          .OrderBy(s => s.Name)
          .Select(s => $"  - {s.Name}: {s.Description}"));

    public string GetSkillBody(string name)
    {
      if ( _skills.TryGetValue(name, out var body) )
        return $"<skill name=\"{name}\">\n{body}\n</skill>";

      return "unknown skill";
    }
}
