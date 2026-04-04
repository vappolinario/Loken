namespace Loken.Core;

public class SkillService : ISkillService
{
    private readonly SkillLoader _loader;

    public SkillService(SkillLoader loader)
    {
        _loader = loader;
    }

    public string GetSkills() => _loader.GetSkills();

    public string GetSkillBody(string name) => _loader.GetSkillBody(name);
}

