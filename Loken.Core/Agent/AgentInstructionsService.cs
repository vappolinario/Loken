namespace Loken.Core;

public class AgentInstructionsService : IAgentInstructionsService
{
    private const string FileName = "AGENTS.md";

    private readonly IPathResolver _pathResolver;

    public AgentInstructionsService(IPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public string? LoadInstructions()
    {
        var dir = new DirectoryInfo(_pathResolver.WorkingDirectory);

        while (dir is not null)
        {
            var filePath = Path.Combine(dir.FullName, FileName);

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            dir = dir.Parent;
        }

        return null;
    }
}
